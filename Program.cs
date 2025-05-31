using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Runtime.CompilerServices;

namespace sqlparsergui
{
    public record FieldLocation(string FilePath, int LineNumber);
    public static class SqlDomExtensions
    {
        public static string ToSqlOperator(this BinaryExpressionType type)
        {
            return type switch
            {
                BinaryExpressionType.Add => "+",
                BinaryExpressionType.Subtract => "-",
                BinaryExpressionType.Multiply => "*",
                BinaryExpressionType.Divide => "/",
                BinaryExpressionType.Modulo => "%",
                _ => type.ToString()
            };
        }
    }
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
   public  class SqlProcAnalyzer
    {
        public class AnalysisResult
        {
        }
        public class ProcAnalysisResult : AnalysisResult
        {
            public string ProcedureName { get; set; } = string.Empty;
            public HashSet<string> Tables { get; set; } = new();
            public HashSet<string> JoinFields { get; set; } = new();
            public HashSet<string> CalledProcedures { get; set; } = new();
            public HashSet<string> WhereConditions { get; set; } = new(); // <-- Add this
            public Dictionary<string, (string expr, HashSet<string> tables, FieldLocation location)> OutputFields { get; set; } = new();


        }

        public class TableAnalysisResultt : AnalysisResult
        {
            public string TableName { get; set; } = string.Empty;
            public HashSet<string> FieldNames { get; set; } = new();
            public Dictionary<string, FieldLocation> FieldLocations { get; set; } = new();
        }
        private class TableSchemaVisitor : TSqlFragmentVisitor
        {
            public string FilePath { get; set; } = "";
            public Dictionary<string, FieldLocation> FieldLocations { get; } = new();

            public Dictionary<string, List<string>> TableSchemas { get; } = new(); // <-- Add this

            public override void Visit(CreateTableStatement node)
            {
                var tableName = node.SchemaObjectName.BaseIdentifier.Value;
                var columns = new List<string>();
                foreach (var column in node.Definition.ColumnDefinitions)
                {
                    columns.Add(column.ColumnIdentifier.Value);
                    FieldLocations[column.ColumnIdentifier.Value] = new FieldLocation(
                        FilePath,
                        column.StartLine
                    );
                }
                TableSchemas[tableName] = columns;
                base.Visit(node);
            }
        }
        public List<AnalysisResult> AnalyzeDirectory(string path, Action<string> log)
        {
            var results = new List<AnalysisResult>();

            foreach (var file in Directory.GetFiles(path, "*.sql"))
            {
                log("Processing file: " + file);    
                string sqlText = File.ReadAllText(file);
                var parser = new TSql150Parser(true); // Use the appropriate SQL Server version
                IList<ParseError> errors;

                var fragment = parser.Parse(new StringReader(sqlText), out errors);

                if (errors.Count > 0)
                {
                    Console.WriteLine($"Parse errors in {file}:");
                    foreach (var error in errors)
                    {
                        log($" - ({error.Line},{error.Column}):{error.Message}");
                    }

                }
                // Extract table schemas as TableAnalysisResultt
                var tableSchemaVisitor = new TableSchemaVisitor { FilePath = file };
                fragment.Accept(tableSchemaVisitor);
                foreach (var kvp in tableSchemaVisitor.TableSchemas)
                {
                    results.Add(new TableAnalysisResultt
                    {
                        TableName = kvp.Key,
                        FieldNames = new HashSet<string>(kvp.Value)
                    });
                }

                var visitor = new StoredProcedureVisitor() { FilePath = file };
                fragment.Accept(visitor);

                foreach (var proc in visitor.StoredProcedures)
                {
                    var analysis = new ProcAnalysisResult
                    {
                        ProcedureName = proc.ProcedureName
                    };

                    AnalyzeStatements(proc.Body, analysis,file);
                    results.Add(analysis);
                }
            }


            return results;
        }

        private class OutputFieldsVisitor : TSqlFragmentVisitor
        {

            public string FilePath { get; set; } = "";
            public Dictionary<string, (string expr, HashSet<string> tables, FieldLocation location)> OutputFields { get; set; } = new();
            private readonly Dictionary<string, string> _tableAliases;

            public OutputFieldsVisitor(Dictionary<string, string> tableAliases)
            {
                _tableAliases = tableAliases;
            }

            public override void Visit(SelectScalarExpression node)
            {
                string alias = node.ColumnName?.Value;
                string expr = GetExpressionString(node.Expression);
                var tables = new HashSet<string>();
                CollectTables(node.Expression, tables);

                if (!string.IsNullOrEmpty(alias))
                {
                    var location = new FieldLocation(
                        FilePath,
                        node.StartLine
                    );
                    OutputFields[alias] = (expr, tables, location);
                }
            }

            private void CollectTables(ScalarExpression expr, HashSet<string> tables)
            {
                switch (expr)
                {
                    case ColumnReferenceExpression col:
                        if (col.MultiPartIdentifier.Identifiers.Count > 1)
                        {
                            var alias = col.MultiPartIdentifier.Identifiers[0].Value;
                            if (_tableAliases.TryGetValue(alias, out var realTable))
                                tables.Add(realTable);
                            else
                                tables.Add(alias);
                        }
                        else
                        {
                            tables.Add(col.MultiPartIdentifier.Identifiers[0].Value);
                        }
                        break;
                    case BinaryExpression bin:
                        CollectTables(bin.FirstExpression, tables);
                        CollectTables(bin.SecondExpression, tables);
                        break;
                    case ParenthesisExpression paren:
                        CollectTables(paren.Expression, tables);
                        break;
                        // Add more cases as needed
                }
            }

            private string GetExpressionString(ScalarExpression expr)
            {
                switch (expr)
                {
                    case ColumnReferenceExpression col:
                        if (col.MultiPartIdentifier.Identifiers.Count == 2)
                        {
                            var alias = col.MultiPartIdentifier.Identifiers[0].Value;
                            var colName = col.MultiPartIdentifier.Identifiers[1].Value;
                            if (_tableAliases.TryGetValue(alias, out var realTable))
                                return $"{realTable}.{colName}";
                            else
                                return $"{alias}.{colName}";
                        }
                        return string.Join(".", col.MultiPartIdentifier.Identifiers.Select(i => i.Value));
                    case BinaryExpression bin:
                        return $"{GetExpressionString(bin.FirstExpression)} {bin.BinaryExpressionType.ToSqlOperator()} {GetExpressionString(bin.SecondExpression)}";
                    case Literal lit:
                        return lit.Value;
                    case ParenthesisExpression paren:
                        return $"({GetExpressionString(paren.Expression)})";
                    default:
                        return expr.ToString();
                }
            }
        }
        private void AnalyzeStatements(TSqlFragment fragment, ProcAnalysisResult result,string file)
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
            foreach (var where in visitor.WhereConditions)
            {
                result.WhereConditions.Add(where);
            }
            // Build alias map
            var aliasMap = visitor.GetAliasMap();

            // Collect output fields with expressions
            var outputVisitor = new OutputFieldsVisitor(aliasMap) { FilePath = file };
            fragment.Accept(outputVisitor);
            foreach (var kvp in outputVisitor.OutputFields)
                result.OutputFields[kvp.Key] = kvp.Value;
        }
        private class StoredProcedureVisitor : TSqlFragmentVisitor
        {
            public List<StoredProcedureInfo> StoredProcedures { get; } = new();

            public string FilePath { get; set; } = "";
            public Dictionary<string, FieldLocation> FieldLocations { get; } = new();

            public override void Visit(CreateProcedureStatement node)
            {
                var procedureName = node.ProcedureReference.Name.BaseIdentifier.Value;
                StoredProcedures.Add(new StoredProcedureInfo
                {
                    ProcedureName = procedureName,
                    Body = node.StatementList
                });
                FieldLocations[procedureName] = new FieldLocation(
              FilePath,
              node.StartLine
          );
            }
        }
        private class TableAndJoinVisitor : TSqlFragmentVisitor
        {
            public string ProcedureName { get; set; } = string.Empty;
            public HashSet<string> Tables { get; set; } = new();
            public HashSet<string> JoinFields { get; set; } = new();
            public HashSet<string> CalledProcedures { get; set; } = new();
            public HashSet<string> WhereConditions { get; set; } = new(); // <-- Add this
            private readonly Dictionary<string, string> TableAliases = new();
            public Dictionary<string, string> GetAliasMap() => new Dictionary<string, string>(TableAliases);

            public override void Visit(QuerySpecification node)
            {
                // First pass: Build table aliases
                if (node.FromClause != null)
                {
                    foreach (var table in node.FromClause.TableReferences)
                    {
                        BuildTableAlias(table);
                    }

                    // Second pass: Process joins and other table references
                    foreach (var table in node.FromClause.TableReferences)
                    {
                        table.Accept(this);
                    }
                }
                if (node.WhereClause != null)
                {
                    // NEW: Process WHERE clause for filter conditions
                    if (node.WhereClause != null)
                    {
                        ProcessSearchCondition(node.WhereClause.SearchCondition);
                    }
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
                    string op = comparison.ComparisonType switch
                    {
                        BooleanComparisonType.Equals => "=",
                        BooleanComparisonType.GreaterThan => ">",
                        BooleanComparisonType.LessThan => "<",
                        BooleanComparisonType.GreaterThanOrEqualTo => ">=",
                        BooleanComparisonType.LessThanOrEqualTo => "<=",
                        BooleanComparisonType.NotEqualToBrackets => "<>",
                        BooleanComparisonType.NotEqualToExclamation => "!=",
                        _ => "="
                    };

                    string left = GetExpressionString(comparison.FirstExpression);
                    string right = GetExpressionString(comparison.SecondExpression);

                    // If both sides are columns, treat as join
                    if (comparison.FirstExpression is ColumnReferenceExpression && comparison.SecondExpression is ColumnReferenceExpression)
                    {
                        JoinFields.Add($"{left} {op} {right}");
                    }
                    // If one side is a column and the other is a literal or variable, treat as filter
                    else if (comparison.FirstExpression is ColumnReferenceExpression && (comparison.SecondExpression is Literal || comparison.SecondExpression is VariableReference))
                    {
                        WhereConditions.Add($"{left} {op} {right}");
                    }
                    else if (comparison.SecondExpression is ColumnReferenceExpression && (comparison.FirstExpression is Literal || comparison.FirstExpression is VariableReference))
                    {
                        WhereConditions.Add($"{right} {op} {left}");
                    }
                }
                else if (condition is BooleanBinaryExpression binary)
                {
                    ProcessSearchCondition(binary.FirstExpression);
                    ProcessSearchCondition(binary.SecondExpression);
                }
            }

            private string GetExpressionString(ScalarExpression expr)
            {
                switch (expr)
                {
                    case ColumnReferenceExpression col:
                        return $"{ResolveTableName(col.MultiPartIdentifier)}.{col.MultiPartIdentifier.Identifiers[^1].Value}";
                    case Literal lit:
                        return lit.Value is string s && !s.StartsWith("'") ? $"'{lit.Value}'" : lit.Value;
                    case VariableReference varRef:
                        return varRef.Name;
                    default:
                        return expr.ToString();
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
}