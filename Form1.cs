using static sqlparsergui.SqlProcAnalyzer;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SqlServer.Management.SqlParser.Metadata;
using System.Drawing;

namespace sqlparsergui
{
    public partial class Form1 : Form
    {
        private const string ResultsFilePath = "analysis_results.json";

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

        private void LogMessage(string message)
        {
            Console.WriteLine(message);
            richTextBox1.AppendText(message + Environment.NewLine);
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var analyzer = new SqlProcAnalyzer();
            var results = analyzer.AnalyzeDirectory(@"..\..\..\..\sqlparsergui\sql\");
            analysisResults = results; // Store at class level

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
            var allFields = GetAllTableFields(results);
            var allTables = GetAllTableNames(results);

            allFieldList = allFields.ToList();
            allTableList = allTables.ToList();

            // Build the tableFieldsMap
            tableFieldsMap = new Dictionary<string, HashSet<string>>();
            foreach (var res in results)
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

            // Subscribe to the event (only once)
            cmbInputSourceField.SelectedIndexChanged -= cmbInputSourceField_SelectedIndexChanged;
            cmbInputSourceField.SelectedIndexChanged += cmbInputSourceField_SelectedIndexChanged;

            cmbInputDestField.SelectedIndexChanged -= cmbInputDestField_SelectedIndexChanged;
            cmbInputDestField.SelectedIndexChanged += cmbInputDestField_SelectedIndexChanged;
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

        private void ComboBox_KeyUp_CaseInsensitive(object sender, KeyEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                List<string> sourceList = comboBox == cmbInputSourceField || comboBox == cmbInputDestField
                    ? allFieldList
                    : allTableList;

                string text = comboBox.Text;
                var matches = sourceList
                    .Where(item => item.Contains(text, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                // Only update if there are matches and the user typed something
                if (matches.Length > 0 && !string.IsNullOrEmpty(text))
                {
                    int selStart = comboBox.SelectionStart;
                    comboBox.Items.Clear();
                    comboBox.Items.AddRange(matches);
                    comboBox.DroppedDown = true;
                    comboBox.Text = text;
                    comboBox.SelectionStart = selStart;
                    comboBox.SelectionLength = 0;
                }
                else if (string.IsNullOrEmpty(text))
                {
                    comboBox.Items.Clear();
                    comboBox.Items.AddRange(sourceList.ToArray());
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
            var route = FindRoute(sourceTable, sourceField, destTable, destField,true,false);
            foreach (var step in route)
                LogMessage(step);

            LogMessage("Tables:");
            route = FindRoute(sourceTable, sourceField, destTable, destField, false, true);
            foreach (var step in route)
                LogMessage(step);
        }

        private List<string> FindRoute(
          string sourceTable, string sourceField,
          string destTable, string destField, bool trySps, bool tryTables)
        {
            if (trySps)
            {
                var procsWithBothTables = analysisResults
                    .OfType<ProcAnalysisResult>()
                    .Where(proc => proc.Tables.Contains(sourceTable) && proc.Tables.Contains(destTable))
                    .Select(proc => proc.ProcedureName)
                    .ToList();

                if (procsWithBothTables.Count > 0)
                {
                    return new List<string> {
                $"Direct route found via procedure(s): {string.Join(", ", procsWithBothTables)}"
            };
                }
            }

            if (tryTables)
            {
                // joinGraph[tableA][tableB] = list of (joinCondition, procName)
                var joinGraph = new Dictionary<string, Dictionary<string, List<(string join, string proc)>>>(StringComparer.OrdinalIgnoreCase);

                foreach (var proc in analysisResults.OfType<ProcAnalysisResult>())
                {
                    foreach (var join in proc.JoinFields)
                    {
                        var parts = join.Split(new[] { "==", "=" }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2)
                        {
                            var left = parts[0].Trim();
                            var right = parts[1].Trim();

                            var leftTable = left.Contains('.') ? left.Split('.')[0] : left;
                            var rightTable = right.Contains('.') ? right.Split('.')[0] : right;

                            // Add join from leftTable to rightTable
                            if (!joinGraph.ContainsKey(leftTable))
                                joinGraph[leftTable] = new Dictionary<string, List<(string, string)>>(StringComparer.OrdinalIgnoreCase);
                            if (!joinGraph[leftTable].ContainsKey(rightTable))
                                joinGraph[leftTable][rightTable] = new List<(string, string)>();
                            joinGraph[leftTable][rightTable].Add(($"{left}={right}", proc.ProcedureName));

                            // Add join from rightTable to leftTable (bidirectional)
                            if (!joinGraph.ContainsKey(rightTable))
                                joinGraph[rightTable] = new Dictionary<string, List<(string, string)>>(StringComparer.OrdinalIgnoreCase);
                            if (!joinGraph[rightTable].ContainsKey(leftTable))
                                joinGraph[rightTable][leftTable] = new List<(string, string)>();
                            joinGraph[rightTable][leftTable].Add(($"{right}={left}", proc.ProcedureName));
                        }
                    }
                }

                // BFS to find a path from sourceTable to destTable, tracking join fields and procs
                var queue = new Queue<(List<string> path, List<(string join, string proc)> joins)>();
                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                queue.Enqueue((new List<string> { sourceTable }, new List<(string, string)>()));
                visited.Add(sourceTable);

                while (queue.Count > 0)
                {
                    var (path, joins) = queue.Dequeue();
                    var last = path.Last();

                    if (string.Equals(last, destTable, StringComparison.OrdinalIgnoreCase))
                    {
                        // Build the SQL join statement
                        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        char aliasChar = 'a';
                        foreach (var table in path)
                        {
                            if (!aliases.ContainsKey(table))
                                aliases[table] = aliasChar++.ToString();
                        }

                        // Collect all procs used in the joins
                        var procsUsed = joins.Select(j => j.proc).Distinct().ToList();
                        var sqlLines = new List<string>();
                        sqlLines.Add($"-- joins from {string.Join(",", procsUsed)}");
                        sqlLines.Add($"select *");
                        sqlLines.Add($"from {path[0]} {aliases[path[0]]}");

                        for (int i = 1; i < path.Count; i++)
                        {
                            string from = path[i - 1];
                            string to = path[i];
                            // Find the join used for this hop
                            var joinInfo = joinGraph[from][to]
                                .FirstOrDefault(j => j.join == joins[i - 1].join && j.proc == joins[i - 1].proc);
                            var joinCond = joinInfo.join;
                            // Replace table names with aliases in the join condition
                            foreach (var kvp in aliases)
                            {
                                joinCond = joinCond.Replace($"{kvp.Key}.", $"{kvp.Value}.");
                            }
                            sqlLines.Add($"join {to} {aliases[to]} on {joinCond}");
                        }
                        return sqlLines;
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
                                    var newPath = new List<string>(path) { neighbor };
                                    var newJoins = new List<(string, string)>(joins) { join };
                                    queue.Enqueue((newPath, newJoins));
                                    // Only enqueue the first join for each neighbor to avoid combinatorial explosion
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return new List<string> { "No route found between source and destination." };
        }

    }
}