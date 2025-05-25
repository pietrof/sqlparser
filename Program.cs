using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using static SqlProcAnalyzer;

var analyzer = new SqlProcAnalyzer();
var results = analyzer.AnalyzeDirectory(@"..\..\..\..\sqlparser\sql\");

foreach (AnalysisResult res in results)
{
    if (res is ProcAnalysisResult proc)
    {
        Console.WriteLine($"Stored Procedure: {proc.ProcedureName}");
        Console.WriteLine("Tables Used:");
        foreach (var table in proc.Tables)
            Console.WriteLine($" - {table}");

        Console.WriteLine("Join Fields:");
        foreach (var field in proc.JoinFields)
            Console.WriteLine($" - {field}");

        Console.WriteLine("Called Procedures:");
        foreach (var calledProc in proc.CalledProcedures)
            Console.WriteLine($" - {calledProc}");

        Console.WriteLine();
    }

    if (res is TableAnalysisResultt tab)
    {
        Console.WriteLine($"Table: {tab.TableName}");
        Console.WriteLine("Fields:");
        foreach (var table in tab.FieldNames)
            Console.WriteLine($" - {table}");


        Console.WriteLine();
    }
}

class SqlProcAnalyzer
{
    public class AnalysisResult
    {
    }
    public class ProcAnalysisResult:AnalysisResult
    {
        public string ProcedureName { get; set; } = string.Empty;
        public HashSet<string> Tables { get; set; } = new();
        public HashSet<string> JoinFields { get; set; } = new();
        public HashSet<string> CalledProcedures { get; set; } = new();
    }
    public class TableAnalysisResultt : AnalysisResult
    {
        public string TableName { get; set; } = string.Empty;
        public HashSet<string> FieldNames { get; set; } = new();
    }
    private class TableSchemaVisitor : TSqlFragmentVisitor
    {
        public Dictionary<string, List<string>> TableSchemas { get; } = new();

        public override void Visit(CreateTableStatement node)
        {
            // Extract the table name
            var tableName = node.SchemaObjectName.BaseIdentifier.Value;

            // Extract the column names
            var columns = new List<string>();
            foreach (var column in node.Definition.ColumnDefinitions)
            {
                columns.Add(column.ColumnIdentifier.Value);
            }

            // Store the table schema
            TableSchemas[tableName] = columns;

            base.Visit(node);
        }
    }

    public List<AnalysisResult> AnalyzeDirectory(string path)
    {
        var results = new List<AnalysisResult>();

        foreach (var file in Directory.GetFiles(path, "*.sql"))
        {
            string sqlText = File.ReadAllText(file);
            var parser = new TSql150Parser(false); // Use the appropriate SQL Server version
            IList<ParseError> errors;

            var fragment = parser.Parse(new StringReader(sqlText), out errors);

            if (errors.Count > 0)
            {
                Console.WriteLine($"Parse errors in {file}:");
                foreach (var error in errors)
                {
                    Console.WriteLine($" - {error.Message}");
                }
                continue;
            }
            // Extract table schemas as TableAnalysisResultt
            var tableSchemaVisitor = new TableSchemaVisitor();
            fragment.Accept(tableSchemaVisitor);
            foreach (var kvp in tableSchemaVisitor.TableSchemas)
            {
                results.Add(new TableAnalysisResultt
                {
                    TableName = kvp.Key,
                    FieldNames = new HashSet<string>(kvp.Value)
                });
            }

            var visitor = new StoredProcedureVisitor();
            fragment.Accept(visitor);

            foreach (var proc in visitor.StoredProcedures)
            {
                var analysis = new ProcAnalysisResult
                {
                    ProcedureName = proc.ProcedureName
                };

                AnalyzeStatements(proc.Body, analysis);
                results.Add(analysis);
            }
        }
      

        return results;
    }

    private void AnalyzeStatements(TSqlFragment fragment, ProcAnalysisResult result)
    {
        var visitor = new TableAndJoinVisitor();
        fragment.Accept(visitor);

        foreach (var table in visitor.Tables)
        {
            result.Tables.Add(table);
        }

        foreach (var joinField in visitor.JoinFields)
        {
            result.JoinFields.Add(joinField);
        }

        foreach (var calledProc in visitor.CalledProcedures)
        {
            result.CalledProcedures.Add(calledProc);
        }
    }

    private class StoredProcedureVisitor : TSqlFragmentVisitor
    {
        public List<StoredProcedureInfo> StoredProcedures { get; } = new();

        public override void Visit(CreateProcedureStatement node)
        {
            var procedureName = node.ProcedureReference.Name.BaseIdentifier.Value;
            StoredProcedures.Add(new StoredProcedureInfo
            {
                ProcedureName = procedureName,
                Body = node.StatementList
            });
        }
    }
    private class TableAndJoinVisitor : TSqlFragmentVisitor
    {
        public HashSet<string> Tables { get; } = new();
        public HashSet<string> JoinFields { get; } = new();
        public HashSet<string> CalledProcedures { get; } = new();
        private readonly Dictionary<string, string> TableAliases = new();

        public override void Visit(QuerySpecification node)
        {
            // First pass: Build table aliases
            foreach (var table in node.FromClause.TableReferences)
            {
              
                    BuildTableAlias(table);
                

            }

            // Second pass: Process joins and other table references
            foreach (var table in node.FromClause.TableReferences)
            {
                table.Accept(this);
            }

            base.Visit(node);
        }

        private void BuildTableAlias(TableReference tableReference)
        {
            if (tableReference is NamedTableReference namedTable)
            {
                // Add the table name to the list of tables
                var tableName = namedTable.SchemaObject.BaseIdentifier.Value;
                Tables.Add(tableName);

                // If the table has an alias, add it to the alias dictionary
                if (namedTable.Alias != null)
                {
                    var alias = namedTable.Alias.Value;
                    TableAliases[alias] = tableName;
                }
            }
            else if (tableReference is QualifiedJoin join)
            {
                // Recursively process the left and right table references in the join
                BuildTableAlias(join.FirstTableReference);
                BuildTableAlias(join.SecondTableReference);
            }
            else if (tableReference is QueryDerivedTable derivedTable)
            {
                // Handle derived tables (subqueries with an alias)
                if (derivedTable.Alias != null)
                {
                    var alias = derivedTable.Alias.Value;
                    TableAliases[alias] = "DerivedTable"; // Use a placeholder name for derived tables
                }

                // Recursively process the query inside the derived table
                derivedTable.QueryExpression.Accept(this);
            }
            else if (tableReference is SchemaObjectFunctionTableReference functionTable)
            {
                // Handle function table references
                var functionName = functionTable.SchemaObject.BaseIdentifier.Value;
                Tables.Add(functionName);

                if (functionTable.Alias != null)
                {
                    var alias = functionTable.Alias.Value;
                    TableAliases[alias] = functionName;
                }
            }
        }

        public override void Visit(QualifiedJoin node)
        {
            // Handle join conditions
            ProcessSearchCondition(node.SearchCondition);

            // Recursively visit the left and right tables in the join
            node.FirstTableReference?.Accept(this);
            node.SecondTableReference?.Accept(this);

            base.Visit(node);
        }

        private void ProcessSearchCondition(BooleanExpression condition)
        {
            if (condition is BooleanComparisonExpression comparison)
            {
                // Handle simple comparison expressions
                if (comparison.FirstExpression is ColumnReferenceExpression left &&
                    comparison.SecondExpression is ColumnReferenceExpression right)
                {
                    var leftTable = ResolveTableName(left.MultiPartIdentifier);
                    var rightTable = ResolveTableName(right.MultiPartIdentifier);

                    var leftField = left.MultiPartIdentifier.Identifiers[^1].Value;
                    var rightField = right.MultiPartIdentifier.Identifiers[^1].Value;

                    JoinFields.Add($"{leftTable}.{leftField} == {rightTable}.{rightField}");
                  
                }
            }
            else if (condition is BooleanBinaryExpression binary)
            {
                // Recursively process left and right conditions in a binary expression
                ProcessSearchCondition(binary.FirstExpression);
                ProcessSearchCondition(binary.SecondExpression);
            }
        }

        private string ResolveTableName(MultiPartIdentifier identifier)
        {
            // If the identifier has a table alias, resolve it to the actual table name
            if (identifier.Identifiers.Count > 1)
            {
                var alias = identifier.Identifiers[0].Value;
                if (TableAliases.TryGetValue(alias, out var tableName))
                {
                    return tableName;
                }
            }

            // If no alias is found, return the identifier as-is
            return identifier.Identifiers[0].Value;
        }

        public override void Visit(CommonTableExpression node)
        {
            // Recursively visit the CTE query
            node.QueryExpression.Accept(this);
            base.Visit(node);
        }

        public override void Visit(ExecuteStatement node)
        {
            // Capture procedure calls
            if (node.ExecuteSpecification.ExecutableEntity is ExecutableProcedureReference procRef)
            {
                var procName = string.Join(".", procRef.ProcedureReference.ProcedureReference.Name.Identifiers.Select(id => id.Value));
                CalledProcedures.Add(procName);
            }

            base.Visit(node);
        }
    


}

private class StoredProcedureInfo
    {
        public string ProcedureName { get; set; } = string.Empty;
        public TSqlFragment Body { get; set; }
    }
}
