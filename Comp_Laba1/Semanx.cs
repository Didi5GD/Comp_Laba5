using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Comp_Laba1
{
    public class SemanticErrorInfo
    {
        public string ErrorMessage { get; set; }
        public string Position { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public string Fragment { get; set; }

        public override string ToString()
        {
            return $"[{Position}] {ErrorMessage}";
        }
    }

    // Базовый класс для AST узлов
    [JsonDerivedType(typeof(IfNode))]
    [JsonDerivedType(typeof(ConditionNode))]
    [JsonDerivedType(typeof(BlockNode))]
    [JsonDerivedType(typeof(StatementNode))]
    [JsonDerivedType(typeof(VariableNode))]
    [JsonDerivedType(typeof(RootNode))]
    public abstract class AstNode
    {
        [JsonPropertyName("nodeType")]
        public string NodeType { get; set; }

        [JsonPropertyName("children")]
        public List<AstNode> Children { get; set; } = new List<AstNode>();

        [JsonPropertyName("attributes")]
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

        protected AstNode(string nodeType)
        {
            NodeType = nodeType;
        }
    }

    public class IfNode : AstNode
    {
        [JsonPropertyName("condition")]
        public ConditionNode Condition { get; set; }

        [JsonPropertyName("thenBlock")]
        public BlockNode ThenBlock { get; set; }

        [JsonPropertyName("elseBlock")]
        public BlockNode ElseBlock { get; set; }

        [JsonPropertyName("line")]
        public int Line { get; set; }

        [JsonPropertyName("column")]
        public int Column { get; set; }

        public IfNode() : base("IfStatement") { }
    }

    // Узел для условия
    public class ConditionNode : AstNode
    {
        [JsonPropertyName("variables")]
        public List<string> Variables { get; set; } = new List<string>();

        [JsonPropertyName("operator")]
        public string Operator { get; set; }

        [JsonPropertyName("leftValue")]
        public string LeftValue { get; set; }

        [JsonPropertyName("rightValue")]
        public string RightValue { get; set; }

        public ConditionNode() : base("Condition") { }
    }

    // Узел для блока кода
    public class BlockNode : AstNode
    {
        [JsonPropertyName("statements")]
        public List<StatementNode> Statements { get; set; } = new List<StatementNode>();

        public BlockNode() : base("Block") { }
    }

    // Узел для оператора
    public class StatementNode : AstNode
    {
        [JsonPropertyName("variable")]
        public string Variable { get; set; }

        [JsonPropertyName("operator")]
        public string Operator { get; set; }

        [JsonPropertyName("rightValue")]
        public string RightValue { get; set; }

        [JsonPropertyName("line")]
        public int Line { get; set; }

        [JsonPropertyName("column")]
        public int Column { get; set; }

        public StatementNode() : base("Statement") { }
    }

    // Узел для переменной
    public class VariableNode : AstNode
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        public VariableNode(string name) : base("Variable")
        {
            Name = name;
            Attributes["name"] = name;
        }
    }

    // Корневой узел для хранения нескольких if-конструкций
    public class RootNode : AstNode
    {
        [JsonPropertyName("ifStatements")]
        public List<IfNode> IfStatements { get; set; } = new List<IfNode>();

        public RootNode() : base("Root") { }
    }

    public class SemanticAnalysisResult
    {
        public AstNode Ast { get; set; }
        public List<SemanticErrorInfo> Errors { get; set; } = new List<SemanticErrorInfo>();

        public string AstJson
        {
            get
            {
                if (Ast == null) return "{}";
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                return JsonSerializer.Serialize(Ast, options);
            }
        }

        public bool HasErrors => Errors.Count > 0;
        public int ErrorCount => Errors.Count;
    }

    public class SemanticAnalyzer
    {
        private List<ScanTokin> _tokens;
        private int _tokenIndex;

        public SemanticAnalyzer(List<ScanTokin> tokens)
        {
            _tokens = tokens;
        }

        public SemanticAnalysisResult Analyze(Parser parser)
        {
            var result = new SemanticAnalysisResult();

            try
            {
                // Проверяем синтаксические ошибки
                var syntaxErrors = parser.GetErrors();
                if (syntaxErrors.Any())
                {
                    foreach (var err in syntaxErrors)
                    {
                        result.Errors.Add(new SemanticErrorInfo
                        {
                            ErrorMessage = err.Description,
                            Position = err.Location,
                            Fragment = err.Fragment,
                            Line = ExtractLine(err.Location),
                            Column = ExtractColumn(err.Location)
                        });
                    }
                    return result;
                }

                // Строим AST (теперь может быть несколько if-конструкций)
                result.Ast = BuildAst();

                // Выполняем семантические проверки
                ValidateSemantics(result.Ast, result.Errors);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new SemanticErrorInfo
                {
                    ErrorMessage = $"Ошибка анализа: {ex.Message}",
                    Position = "(?, ?)",
                    Fragment = "",
                    Line = 0,
                    Column = 0
                });
            }

            return result;
        }

        private AstNode BuildAst()
        {
            _tokenIndex = 0;

            // Создаем корневой узел, который будет содержать все конструкции
            var root = new RootNode();

            // Парсим все if-конструкции подряд
            while (_tokenIndex < _tokens.Count)
            {
                // Пропускаем возможные точки с запятой между конструкциями
                while (_tokenIndex < _tokens.Count && _tokens[_tokenIndex].Type == "SEMICOLON")
                {
                    _tokenIndex++;
                }

                if (_tokenIndex >= _tokens.Count) break;

                // Если встретили IF, парсим конструкцию
                if (_tokens[_tokenIndex].Type == "IF")
                {
                    var ifNode = ParseIfStatement();
                    root.IfStatements.Add(ifNode);
                    root.Children.Add(ifNode);
                }
                else
                {
                    // Если не IF, просто двигаемся дальше
                    _tokenIndex++;
                }
            }

            return root;
        }

        private IfNode ParseIfStatement()
        {
            var ifNode = new IfNode();

            // Сохраняем позицию IF для отладки
            if (_tokenIndex < _tokens.Count && _tokens[_tokenIndex].Type == "IF")
            {
                ifNode.Line = _tokens[_tokenIndex].Line;
                ifNode.Column = _tokens[_tokenIndex].Column;
                _tokenIndex++;
            }

            if (_tokenIndex < _tokens.Count && _tokens[_tokenIndex].Type == "LPAREN")
                _tokenIndex++;

            ifNode.Condition = ParseCondition();
            ifNode.Children.Add(ifNode.Condition);

            if (_tokenIndex < _tokens.Count && _tokens[_tokenIndex].Type == "RPAREN")
                _tokenIndex++;

            ifNode.ThenBlock = ParseBlock();
            ifNode.Children.Add(ifNode.ThenBlock);

            if (_tokenIndex < _tokens.Count && _tokens[_tokenIndex].Type == "ELSE")
            {
                _tokenIndex++;
                ifNode.ElseBlock = ParseBlock();
                ifNode.Children.Add(ifNode.ElseBlock);
            }

            // Пропускаем ';' после конструкции
            if (_tokenIndex < _tokens.Count && _tokens[_tokenIndex].Type == "SEMICOLON")
                _tokenIndex++;

            return ifNode;
        }

        private ConditionNode ParseCondition()
        {
            var condition = new ConditionNode();

            if (_tokenIndex < _tokens.Count && _tokens[_tokenIndex].Type == "IDENTIFIER")
            {
                condition.LeftValue = _tokens[_tokenIndex].Lecsema;
                condition.Variables.Add(condition.LeftValue);
                condition.Children.Add(new VariableNode(condition.LeftValue));
                _tokenIndex++;
            }

            if (_tokenIndex < _tokens.Count && IsRelationalOperator(_tokens[_tokenIndex].Type))
            {
                condition.Operator = _tokens[_tokenIndex].Lecsema;
                _tokenIndex++;
            }

            if (_tokenIndex < _tokens.Count && _tokens[_tokenIndex].Type == "IDENTIFIER")
            {
                condition.RightValue = _tokens[_tokenIndex].Lecsema;
                condition.Variables.Add(condition.RightValue);
                condition.Children.Add(new VariableNode(condition.RightValue));
                _tokenIndex++;
            }

            return condition;
        }

        private BlockNode ParseBlock()
        {
            var block = new BlockNode();

            if (_tokenIndex < _tokens.Count && _tokens[_tokenIndex].Type == "LBRACE")
                _tokenIndex++;

            while (_tokenIndex < _tokens.Count && _tokens[_tokenIndex].Type != "RBRACE")
            {
                var statement = ParseStatement();
                if (statement != null)
                {
                    block.Statements.Add(statement);
                    block.Children.Add(statement);
                }

                // Защита от бесконечного цикла
                if (_tokenIndex >= _tokens.Count) break;
            }

            if (_tokenIndex < _tokens.Count && _tokens[_tokenIndex].Type == "RBRACE")
                _tokenIndex++;

            return block;
        }

        private StatementNode ParseStatement()
        {
            var statement = new StatementNode();

            if (_tokenIndex < _tokens.Count)
            {
                statement.Line = _tokens[_tokenIndex].Line;
                statement.Column = _tokens[_tokenIndex].Column;
            }

            if (_tokenIndex < _tokens.Count && _tokens[_tokenIndex].Type == "IDENTIFIER")
            {
                statement.Variable = _tokens[_tokenIndex].Lecsema;
                _tokenIndex++;
            }
            else
            {
                return null;
            }

            if (_tokenIndex < _tokens.Count)
            {
                string opType = _tokens[_tokenIndex].Type;
                if (opType == "ASSIGN")
                {
                    statement.Operator = "=";
                    _tokenIndex++;

                    if (_tokenIndex < _tokens.Count && _tokens[_tokenIndex].Type == "IDENTIFIER")
                    {
                        statement.RightValue = _tokens[_tokenIndex].Lecsema;
                        _tokenIndex++;
                    }
                }
                else if (opType == "INCREMENT")
                {
                    statement.Operator = "++";
                    _tokenIndex++;
                }
                else if (opType == "DECREMENT")
                {
                    statement.Operator = "--";
                    _tokenIndex++;
                }
            }

            if (_tokenIndex < _tokens.Count && _tokens[_tokenIndex].Type == "SEMICOLON")
                _tokenIndex++;

            return statement;
        }

        private void ValidateSemantics(AstNode ast, List<SemanticErrorInfo> errors)
        {
            if (ast == null) return;

            // Если это корневой узел, проверяем все его if-конструкции
            if (ast is RootNode root)
            {
                foreach (var ifNode in root.IfStatements)
                {
                    ValidateIfNode(ifNode, errors);
                }
            }
            // Если это прямой IfNode (для обратной совместимости)
            else if (ast is IfNode ifNode)
            {
                ValidateIfNode(ifNode, errors);
            }

        }

        private void ValidateIfNode(IfNode ifNode, List<SemanticErrorInfo> errors)
        {
            var conditionVars = ifNode.Condition?.Variables ?? new List<string>();

            // Очищаем переменные от символа $, если он есть
            var cleanConditionVars = conditionVars.Select(v => v.TrimStart('$')).ToList();

            if (cleanConditionVars.Count == 0)
            {
                errors.Add(new SemanticErrorInfo
                {
                    ErrorMessage = "В условии отсутствуют идентификаторы",
                    Position = GetPosition(ifNode),
                    Fragment = "",
                    Line = ifNode.Line,
                    Column = ifNode.Column
                });
                return;
            }

            // Получаем использованные переменные в каждом блоке
            var thenVars = GetBlockVariables(ifNode.ThenBlock);
            var elseVars = ifNode.ElseBlock != null ? GetBlockVariables(ifNode.ElseBlock) : new HashSet<string>();

            // Проверяем блок then
            var missingInThen = cleanConditionVars.Where(v => !thenVars.Contains(v)).ToList();
            // Если ВСЕ переменные из условия отсутствуют в блоке then, то ошибка
            if (missingInThen.Count == cleanConditionVars.Count)
            {
                errors.Add(new SemanticErrorInfo
                {
                    ErrorMessage = $"В блоке then отсутствуют идентификаторы из условия: {string.Join(", ", missingInThen)}",
                    Position = GetPosition(ifNode.ThenBlock),
                    Fragment = string.Join(", ", missingInThen),
                    Line = ifNode.Line,
                    Column = ifNode.Column
                });
            }

            // Проверяем блок else (если есть)
            if (ifNode.ElseBlock != null)
            {
                var missingInElse = cleanConditionVars.Where(v => !elseVars.Contains(v)).ToList();
                // Если ВСЕ переменные из условия отсутствуют в блоке else, то ошибка
                if (missingInElse.Count == cleanConditionVars.Count)
                {
                    errors.Add(new SemanticErrorInfo
                    {
                        ErrorMessage = $"В блоке else отсутствуют идентификаторы из условия: {string.Join(", ", missingInElse)}",
                        Position = GetPosition(ifNode.ElseBlock),
                        Fragment = string.Join(", ", missingInElse),
                        Line = ifNode.Line,
                        Column = ifNode.Column
                    });
                }
            }
        }

        private HashSet<string> GetBlockVariables(BlockNode block)
        {
            var variables = new HashSet<string>();

            if (block == null) return variables;

            foreach (var stmt in block.Statements)
            {
                if (!string.IsNullOrEmpty(stmt.Variable))
                    variables.Add(stmt.Variable.TrimStart('$')); // Очищаем от $
                if (!string.IsNullOrEmpty(stmt.RightValue))
                    variables.Add(stmt.RightValue.TrimStart('$')); // Очищаем от $
            }

            return variables;
        }

        private bool IsRelationalOperator(string tokenType)
        {
            string[] ops = { "LESS", "LESSEQ", "GREATER", "GREATEREQ", "EQUAL", "NEQUAL" };
            return ops.Contains(tokenType);
        }

        private string GetPosition(AstNode node)
        {
            if (node is IfNode ifNode && ifNode.Line > 0)
            {
                return $"({ifNode.Line}, {ifNode.Column})";
            }
            if (node is BlockNode block && block.Statements.Count > 0)
            {
                var firstStmt = block.Statements.FirstOrDefault();
                if (firstStmt != null && firstStmt.Line > 0)
                {
                    return $"({firstStmt.Line}, {firstStmt.Column})";
                }
            }
            return "(?, ?)";
        }

        private int ExtractLine(string location)
        {
            if (string.IsNullOrEmpty(location)) return 0;
            var parts = location.Trim('(', ')').Split(',');
            if (parts.Length >= 1 && int.TryParse(parts[0], out int line))
                return line;
            return 0;
        }

        private int ExtractColumn(string location)
        {
            if (string.IsNullOrEmpty(location)) return 0;
            var parts = location.Trim('(', ')').Split(',');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int col))
                return col;
            return 0;
        }
    }
}