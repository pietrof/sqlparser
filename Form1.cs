using static sqlparsergui.SqlProcAnalyzer;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SqlServer.Management.SqlParser.Metadata;
using System.Drawing;
using System.Diagnostics;

namespace sqlparsergui
{
    public partial class Form1 : Form
    {
        private const string ResultsFilePath = "analysis_results.json";
        private Dictionary<string, FieldLocation> fieldLocations = new();
        // Store the results at class level
        private List<AnalysisResult> analysisResults = new();

        private List<string> allFieldList = new();
        private List<string> allTableList = new();
        private Dictionary<string, HashSet<string>> tableFieldsMap = new();

        public Form1()
        {
            InitializeComponent();
            RestoreAnalysisResults();
        }

        public void LogMessage(string message)
        {
            Console.WriteLine(message);
            richTextBox1.AppendText(message + Environment.NewLine);
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
        }
        IEnumerable<ProcAnalysisResult> allProcs;
        private void button1_Click(object sender, EventArgs e)
        {
            var analyzer = new SqlProcAnalyzer(); 
            var results = analyzer.AnalyzeDirectory(@"..\..\..\..\sqlparsergui\sql\", LogMessage);
            analysisResults = results; // Store at class level
                                       // For table fields



            var allFields = GetAllTableFields(results);
            var allTables = GetAllTableNames(results);
            allProcs = GetAllProcs(results);

            allFieldList = allFields.ToList();
            allTableList = allTables.ToList();


            foreach (var res in results)
            {
                if (res is TableAnalysisResultt tab)
                {
                    foreach (var kvp in tab.FieldLocations)
                        fieldLocations[kvp.Key] = kvp.Value;
                }
            }

            // For calculated fields
            foreach (var proc in allProcs)
            {
                foreach (var field in proc.OutputFields)
                {
                    var key = $"{field.Key}({field.Value.expr})";
                    fieldLocations[key] = field.Value.location;
                }
            }

            SaveAnalysisResults();

            foreach (AnalysisResult res in results)
            {
                if (res is ProcAnalysisResult proc)
                {
                    LogMessage($"Stored Procedure: {proc.ProcedureName}");
                    LogMessage("Tables Used:");
                    foreach (var table in proc.Tables)
                        LogMessage($" - {table}");

                    LogMessage("Join Fields:");
                    foreach (var field in proc.JoinFields)
                        LogMessage($" - {field}");

                    LogMessage("Called Procedures:");
                    foreach (var calledProc in proc.CalledProcedures)
                        LogMessage($" - {calledProc}");

                    LogMessage(string.Empty);
                }

                if (res is TableAnalysisResultt tab)
                {
                    LogMessage($"Table: {tab.TableName}");
                    LogMessage("Fields:");
                    foreach (var table in tab.FieldNames)
                        LogMessage($" - {table}");

                    LogMessage(string.Empty);
                }
            }
          
            // Build the tableFieldsMap
            tableFieldsMap = new Dictionary<string, HashSet<string>>();
            foreach (var res in results)
            {
                if (res is TableAnalysisResultt tab)
                {
                    tableFieldsMap[tab.TableName] = tab.FieldNames;
                }
            }
            var combinedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Optionally, add all fields from all tables used in the proc
            foreach (var proc in allProcs)
            {
                // Add calculated/output fields with expressions
                foreach (var field in proc.OutputFields)
                    combinedFields.Add($"{field.Key}({field.Value.expr})");

                // Add all fields from all tables used in the proc
                foreach (var tableName in proc.Tables)
                {
                    if (tableFieldsMap.TryGetValue(tableName, out var tableFields))
                    {
                        foreach (var field in tableFields)
                            combinedFields.Add(field);
                    }
                }
            }
            // Now populate the ComboBox
            cmbInputDestField.Items.Clear();
            cmbInputDestField.Items.AddRange(combinedFields.ToArray());
            destFieldFullList = combinedFields.ToList();

            SetupComboBoxLookahead(cmbInputSourceField, allFieldList);
            SetupComboBoxLookahead(cmbInputDestField, destFieldFullList);
            SetupComboBoxLookahead(cmbInputSourceTable, allTableList);
            SetupComboBoxLookahead(cmbInputDestTable, allTableList);

            // Subscribe to the event (only once)
            cmbInputSourceField.SelectedIndexChanged -= cmbInputSourceField_SelectedIndexChanged;
            cmbInputSourceField.SelectedIndexChanged += cmbInputSourceField_SelectedIndexChanged;

            cmbInputDestField.SelectedIndexChanged -= cmbInputDestField_SelectedIndexChanged;
            cmbInputDestField.SelectedIndexChanged += cmbInputDestField_SelectedIndexChanged;
        }
        private void btnJumpToDefinition_Click(object sender, EventArgs e)
        {
            string selectedField = cmbInputDestField.Text;
            if (fieldLocations.TryGetValue(selectedField, out var location))
            {
                if (!string.IsNullOrEmpty(location.FilePath))
                {
                    // Open the file at the specified line using the default editor (e.g., Notepad++)
                    // You can use Notepad++ or VSCode for better line navigation, or fallback to Notepad
                    string editor = "C:\\Program Files\\Notepad++\\notepad++.exe";

                    // For Notepad++ or VSCode, you can add line number support:
                    // string editor = "notepad++.exe";
                    string args = $"-n{location.LineNumber} \"{location.FilePath}\"";
                    Process.Start(editor, args);
                }
                else
                {
                    MessageBox.Show("File path not available for this field.");
                }
            }
            else
            {
                MessageBox.Show("No location info for selected field.");
            }
        }
        private IEnumerable<ProcAnalysisResult> GetAllProcs(List<AnalysisResult> results)
        {
            var allFields = new HashSet<ProcAnalysisResult>();
            foreach (var res in results)
            {
                if (res is ProcAnalysisResult tab)
                {
                    {
                        allFields.Add(tab);
                    }
                }
            }
            return allFields;
        }

        private void SaveAnalysisResults()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new AnalysisResultJsonConverter() }
            };
            File.WriteAllText(ResultsFilePath, JsonSerializer.Serialize(analysisResults, options));
        }

        private void RestoreAnalysisResults()
        {
            if (File.Exists(ResultsFilePath))
            {
                var options = new JsonSerializerOptions
                {
                    Converters = { new AnalysisResultJsonConverter() }
                };
                var json = File.ReadAllText(ResultsFilePath);
                analysisResults = JsonSerializer.Deserialize<List<AnalysisResult>>(json, options) ?? new List<AnalysisResult>();

                // Rebuild allFieldList, allTableList, and tableFieldsMap
                var allFields = GetAllTableFields(analysisResults);
                var allTables = GetAllTableNames(analysisResults);

                allFieldList = allFields.ToList();
                allTableList = allTables.ToList();

                tableFieldsMap = new Dictionary<string, HashSet<string>>();
                foreach (var res in analysisResults)
                {
                    if (res is TableAnalysisResultt tab)
                    {
                        tableFieldsMap[tab.TableName] = tab.FieldNames;
                    }
                }

                SetupComboBoxLookahead(cmbInputSourceField, allFieldList);
                SetupComboBoxLookahead(cmbInputDestField, allFieldList);
                SetupComboBoxLookahead(cmbInputSourceTable, allTableList);
                SetupComboBoxLookahead(cmbInputDestTable, allTableList);

                cmbInputSourceField.SelectedIndexChanged -= cmbInputSourceField_SelectedIndexChanged;
                cmbInputSourceField.SelectedIndexChanged += cmbInputSourceField_SelectedIndexChanged;

                cmbInputDestField.SelectedIndexChanged -= cmbInputDestField_SelectedIndexChanged;
                cmbInputDestField.SelectedIndexChanged += cmbInputDestField_SelectedIndexChanged;
            }
        }

        private void cmbInputSourceField_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedField = cmbInputSourceField.Text;
            List<string> filteredTables;

            if (string.IsNullOrWhiteSpace(selectedField))
            {
                // Show all tables if no field is selected
                filteredTables = allTableList;
            }
            else
            {
                // Find tables that contain the selected field (case-insensitive)
                filteredTables = tableFieldsMap
                    .Where(kvp => kvp.Value.Any(f => f.Equals(selectedField, StringComparison.OrdinalIgnoreCase)))
                    .Select(kvp => kvp.Key)
                    .ToList();

                // If no tables found, show all tables (or you can choose to show none)
                if (filteredTables.Count == 0)
                    filteredTables = allTableList;
            }

            SetupComboBoxLookahead(cmbInputSourceTable, filteredTables);
            // Automatically select the first table if available
            if (filteredTables.Count > 0)
                cmbInputSourceTable.SelectedIndex = 0;
        }

        private void cmbInputDestField_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedField = cmbInputDestField.Text;
            List<string> filteredTables;

            if (string.IsNullOrWhiteSpace(selectedField))
            {
                // Show all tables if no field is selected
                filteredTables = allTableList;
            }
            else
            {
                // Find tables that contain the selected field (case-insensitive)
                filteredTables = tableFieldsMap
                    .Where(kvp => kvp.Value.Any(f => f.Equals(selectedField, StringComparison.OrdinalIgnoreCase)))
                    .Select(kvp => kvp.Key)
                    .ToList();

                // If no tables found, show all tables (or you can choose to show none)
                if (filteredTables.Count == 0)
                    filteredTables = allTableList;
            }

            SetupComboBoxLookahead(cmbInputDestTable, filteredTables);
            // Automatically select the first table if available
            if (filteredTables.Count > 0)
                cmbInputDestTable.SelectedIndex = 0;
        }

        private void SetupComboBoxLookahead(ComboBox comboBox, List<string> items)
        {
            comboBox.Items.Clear();
            comboBox.Items.AddRange(items.ToArray());
            comboBox.AutoCompleteMode = AutoCompleteMode.None; // We'll handle it manually
            comboBox.AutoCompleteSource = AutoCompleteSource.None;
            comboBox.KeyUp -= ComboBox_KeyUp_CaseInsensitive; // Prevent multiple subscriptions
            comboBox.KeyUp += ComboBox_KeyUp_CaseInsensitive;
        }
        private List<string> destFieldFullList = new();
        private void ComboBox_KeyUp_CaseInsensitive(object sender, KeyEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                List<string> sourceList;

                // Use the correct list for each ComboBox
                if (comboBox == cmbInputSourceField)
                    sourceList = allFieldList;
                else if (comboBox == cmbInputDestField)
                    sourceList = destFieldFullList; // Use the full list, not Items
                else
                    sourceList = allTableList;

                string text = comboBox.Text;

                // If the text is empty, show the full list and close the dropdown
                if (string.IsNullOrEmpty(text))
                {
                    comboBox.Items.Clear();
                    comboBox.Items.AddRange(sourceList.ToArray());
                    comboBox.DroppedDown = false;
                    return;
                }

                var matches = sourceList
                    .Where(item => item.Contains(text, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                // Only update if there are matches and the user typed something
                if (matches.Length > 0)
                {
                    int selStart = comboBox.SelectionStart;
                    comboBox.Items.Clear();
                    comboBox.Items.AddRange(matches);
                    comboBox.DroppedDown = true;
                    comboBox.Text = text;
                    comboBox.SelectionStart = selStart;
                    comboBox.SelectionLength = 0;
                }
                else
                {
                    // If no matches, keep the dropdown closed and show nothing
                    comboBox.DroppedDown = false;
                }
            }
        }

        private HashSet<string> GetAllTableFields(IEnumerable<AnalysisResult> results)
        {
            var allFields = new HashSet<string>();
            foreach (var res in results)
            {
                if (res is TableAnalysisResultt tab)
                {
                    foreach (var field in tab.FieldNames)
                    {
                        allFields.Add(field);
                    }
                }
            }
            return allFields;
        }

        private HashSet<string> GetAllTableNames(IEnumerable<AnalysisResult> results)
        {
            var allTables = new HashSet<string>();
            foreach (var res in results)
            {
                if (res is TableAnalysisResultt tab)
                {
                    allTables.Add(tab.TableName);
                }
            }
            return allTables;
        }

        // Custom converter for polymorphic serialization/deserialization
        public class AnalysisResultJsonConverter : JsonConverter<AnalysisResult>
        {
            public override AnalysisResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                using (var doc = JsonDocument.ParseValue(ref reader))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("ProcedureName", out _))
                        return JsonSerializer.Deserialize<ProcAnalysisResult>(root.GetRawText(), options)!;
                    if (root.TryGetProperty("TableName", out _))
                        return JsonSerializer.Deserialize<TableAnalysisResultt>(root.GetRawText(), options)!;
                    throw new JsonException("Unknown AnalysisResult type.");
                }
            }

            public override void Write(Utf8JsonWriter writer, AnalysisResult value, JsonSerializerOptions options)
            {
                if (value is ProcAnalysisResult proc)
                    JsonSerializer.Serialize(writer, proc, options);
                else if (value is TableAnalysisResultt tab)
                    JsonSerializer.Serialize(writer, tab, options);
                else
                    throw new JsonException("Unknown AnalysisResult type.");
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            var sourceTable = cmbInputSourceTable.Text;
            var sourceField = cmbInputSourceField.Text;
            var destTable = cmbInputDestTable.Text;
            var destField = cmbInputDestField.Text;

            LogMessage("StoredProcs:");
            var routes = FindRoutes(sourceTable, sourceField, destTable, destField, true, false);
            foreach (var route in routes)
            {
                foreach (var step in route)
                    LogMessage(step);
                LogMessage(""); // Blank line between routes
            }

            LogMessage("Tables:");
            routes = FindRoutes(sourceTable, sourceField, destTable, destField, false, true);
            foreach (var route in routes)
            {
                foreach (var step in route)
                    LogMessage(step);
                LogMessage(""); // Blank line between routes
            }
        }
        private HashSet<string> GetTablesForField(string fieldName)
        {
            var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);



            foreach (var proc in allProcs)
            {
                // Add calculated/output fields with expressions
                foreach (var field in proc.OutputFields)
                {
                    if (fieldName == $"{field.Key}({field.Value.expr})")
                    {
                        tables = field.Value.tables;
                    }
                }


            }


            // Check if it's a direct table field
            foreach (var kvp in tableFieldsMap)
            {
                if (kvp.Value.Contains(fieldName))
                    tables.Add(kvp.Key);
            }

            // Check if it's a calculated field in any proc
            foreach (var proc in analysisResults.OfType<ProcAnalysisResult>())
            {
                if (proc.OutputFields.TryGetValue(fieldName, out var val))
                {
                    foreach (var t in val.tables)
                        tables.Add(t);
                }
            }

            return tables;
        }
        private List<List<string>> FindRoutes(
         string sourceTable, string sourceField,
         string destTable, string destField, bool trySps, bool tryTables)
        {
            var allRoutes = new List<List<string>>();

            // Support calculated fields: resolve to all possible tables
            var sourceTables = !string.IsNullOrWhiteSpace(sourceField)
                ? GetTablesForField(sourceField)
                : new HashSet<string> { sourceTable };
            var destTables = !string.IsNullOrWhiteSpace(destField)
                ? GetTablesForField(destField)
                : new HashSet<string> { destTable };

            // If no tables found, fallback to user selection
            if (sourceTables.Count == 0) sourceTables.Add(sourceTable);
            if (destTables.Count == 0) destTables.Add(destTable);

            foreach (var src in sourceTables)
            {
                foreach (var dest in destTables)
                {
                    if (src.Equals(dest, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (trySps)
                    {
                        var procsWithBothTables = analysisResults
                            .OfType<ProcAnalysisResult>()
                            .Where(proc => proc.Tables.Contains(src) && proc.Tables.Contains(dest))
                            .Select(proc => proc.ProcedureName)
                            .ToList();

                        if (procsWithBothTables.Count > 0)
                        {
                            allRoutes.Add(new List<string> {
                $"Direct route found via procedure(s): {string.Join(", ", procsWithBothTables)}"
            });
                        }
                    }

                    if (tryTables)
                    {
                        // joinGraph[tableA][tableB] = list of (joinCondition, procName)
                        var joinGraph = new Dictionary<string, Dictionary<string, List<(string join, string proc)>>>(StringComparer.OrdinalIgnoreCase);

                        // Map: procName -> where conditions
                        var procWhereConditions = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

                        foreach (var proc in analysisResults.OfType<ProcAnalysisResult>())
                        {
                            // Store where conditions for this proc
                            if (proc.WhereConditions != null && proc.WhereConditions.Count > 0)
                                procWhereConditions[proc.ProcedureName] = proc.WhereConditions;

                            foreach (var join in proc.JoinFields)
                            {
                                var parts = join.Split(new[] { "==", "=" }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length == 2)
                                {
                                    var left = parts[0].Trim();
                                    var right = parts[1].Trim();

                                    var leftTable = left.Contains('.') ? left.Split('.')[0] : left;
                                    var rightTable = right.Contains('.') ? right.Split('.')[0] : right;

                                    if (!joinGraph.ContainsKey(leftTable))
                                        joinGraph[leftTable] = new Dictionary<string, List<(string, string)>>(StringComparer.OrdinalIgnoreCase);
                                    if (!joinGraph[leftTable].ContainsKey(rightTable))
                                        joinGraph[leftTable][rightTable] = new List<(string, string)>();
                                    joinGraph[leftTable][rightTable].Add(($"{left}={right}", proc.ProcedureName));

                                    if (!joinGraph.ContainsKey(rightTable))
                                        joinGraph[rightTable] = new Dictionary<string, List<(string, string)>>(StringComparer.OrdinalIgnoreCase);
                                    if (!joinGraph[rightTable].ContainsKey(leftTable))
                                        joinGraph[rightTable][leftTable] = new List<(string, string)>();
                                    joinGraph[rightTable][leftTable].Add(($"{right}={left}", proc.ProcedureName));
                                }
                            }
                        }

                        // DFS to find all paths from src to dest, tracking join fields and procs
                        void Dfs(
                            List<string> path,
                            List<(string join, string proc)> joins,
                            HashSet<string> visited)
                        {
                            var last = path.Last();
                            if (string.Equals(last, dest, StringComparison.OrdinalIgnoreCase))
                            {
                                // Build the SQL join statement
                                var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                char aliasChar = 'a';
                                foreach (var table in path)
                                {
                                    if (!aliases.ContainsKey(table))
                                        aliases[table] = aliasChar++.ToString();
                                }

                                var procsUsed = joins.Select(j => j.proc).Distinct().ToList();
                                var sqlLines = new List<string>();
                                sqlLines.Add($"-- joins from {string.Join(",", procsUsed)}");

                                // Determine the SELECT clause for source and dest fields
                                string sourceAlias = aliases[path[0]];
                                string destAlias = aliases[path[^1]];

                                // Handle computed destField
                                string selectSource, selectDest;

                                // Try to find the computed destField expression
                                // Try to find the computed destField expression
                                (string expr, HashSet<string> tables)? destFieldExpr = null;
                                string destFieldNameOnly = destField;
                                int parenIdx = destField.IndexOf('(');
                                if (parenIdx > 0)
                                    destFieldNameOnly = destField.Substring(0, parenIdx);

                                foreach (var proc in analysisResults.OfType<ProcAnalysisResult>())
                                {
                                    foreach (var of in proc.OutputFields)
                                    {
                                        // Match on the combined field name (e.g. "FieldName(Expression)")
                                        if ($"{of.Key}({of.Value.expr})".Equals(destField, StringComparison.OrdinalIgnoreCase))
                                        {
                                            destFieldExpr = (of.Value.expr, of.Value.tables);
                                            break;
                                        }
                                    }
                                    if (destFieldExpr != null) break;
                                }

                                if (destFieldExpr != null)
                                {
                                    // Replace table names in the expression with aliases
                                    string exprWithAliases = destFieldExpr.Value.expr;
                                    foreach (var kvp in aliases)
                                    {
                                        exprWithAliases = exprWithAliases.Replace($"{kvp.Key}.", $"{kvp.Value}.");
                                    }
                                    selectDest = $"{exprWithAliases} as [{destFieldNameOnly}]";
                                }
                                else
                                {
                                    // Not a computed field, just use alias.field
                                    selectDest = $"{destAlias}.{destField}";
                                }

                                // Handle source field (for symmetry, you can also check if it's computed, but usually it's a direct field)
                                selectSource = $"{sourceAlias}.{sourceField}";

                                sqlLines.Add($"select {selectSource}, {selectDest}");
                                sqlLines.Add($"from {path[0]} {sourceAlias}");

                                for (int i = 1; i < path.Count; i++)
                                {
                                    string from = path[i - 1];
                                    string to = path[i];
                                    var joinInfo = joins[i - 1];
                                    var joinCond = joinInfo.join;
                                    foreach (var kvp in aliases)
                                    {
                                        joinCond = joinCond.Replace($"{kvp.Key}.", $"{kvp.Value}.");
                                    }
                                    sqlLines.Add($"join {to} {aliases[to]} on {joinCond}");
                                }

                                // Collect WHERE conditions from the procs used in the joins
                                var whereConds = procsUsed
                                    .Where(procWhereConditions.ContainsKey)
                                    .SelectMany(proc => procWhereConditions[proc])
                                    .Distinct()
                                    .ToList();

                                if (whereConds.Count > 0)
                                {
                                    sqlLines.Add("where " + string.Join("\nand ", whereConds.Select(cond =>
                                    {
                                        var condWithAliases = cond;
                                        foreach (var kvp in aliases)
                                        {
                                            condWithAliases = condWithAliases.Replace($"{kvp.Key}.", $"{kvp.Value}.");
                                        }
                                        return condWithAliases;
                                    })));
                                }

                                allRoutes.Add(sqlLines);
                                return;
                            }

                            if (joinGraph.TryGetValue(last, out var neighbors))
                            {
                                foreach (var neighbor in neighbors.Keys)
                                {
                                    if (!visited.Contains(neighbor))
                                    {
                                        foreach (var join in neighbors[neighbor])
                                        {
                                            visited.Add(neighbor);
                                            path.Add(neighbor);
                                            joins.Add(join);
                                            Dfs(path, joins, visited);
                                            path.RemoveAt(path.Count - 1);
                                            joins.RemoveAt(joins.Count - 1);
                                            visited.Remove(neighbor);
                                        }
                                    }
                                }
                            }
                        }

                        Dfs(new List<string> { src }, new List<(string, string)>(), new HashSet<string>(StringComparer.OrdinalIgnoreCase) { src });

                    }
                }
            }

            if (allRoutes.Count == 0)
                allRoutes.Add(new List<string> { "No route found between source and destination." });

            return allRoutes;
        }

        private void cmbInputDestField_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button3_MouseClick(object sender, MouseEventArgs e)
        {
            btnJumpToDefinition_Click(sender, e);


        }
}
}