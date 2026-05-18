using Comp_Laba1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Comp_Laba1
{
    public class AnalysisError
    {
        public string Type { get; set; }
        public string Fragment { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }

        public AnalysisError(string type, string frag, string pos, string message)
        {
            Type = type;
            Fragment = frag;
            Location = pos;
            Description = message;
        }
    }

    public class SemanticResult
    {
        public List<AnalysisError> Errors { get; set; } = new List<AnalysisError>();
        public string AstText { get; set; } = "";
        public int ErrorCount => Errors.Count;
    }

    public abstract class Node
    {
        public abstract string Print(string indent, bool isLast);
    }

    public class StartNode : Node
    {
        public string IfToken { get; set; }
        public ConditionNode Condition { get; set; }
        public BlockNode IfBlock { get; set; }
        public ElsePartNode ElsePart { get; set; }
        public string EndSemi { get; set; }

        public override string Print(string indent, bool isLast)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{indent}{(isLast ? "└─ " : "├─ ")}<Start>");
            string childIndent = indent + (isLast ? "   " : "|  ");
            if (!string.IsNullOrEmpty(IfToken)) sb.AppendLine($"{childIndent}├─ 'if': \"{IfToken}\"");
            if (Condition != null) sb.Append(Condition.Print(childIndent, false));
            if (IfBlock != null) sb.Append(IfBlock.Print(childIndent, false));
            if (ElsePart != null) sb.Append(ElsePart.Print(childIndent, false));
            if (!string.IsNullOrEmpty(EndSemi)) sb.AppendLine($"{childIndent}└─ <END_SEMI>: \"{EndSemi}\"");
            return sb.ToString();
        }
    }

    public class ConditionNode : Node
    {
        public string OpenParen { get; set; }
        public ExprNode Expr { get; set; }
        public string CloseParen { get; set; }

        public override string Print(string indent, bool isLast)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{indent}{(isLast ? "└─ " : "├─ ")}<CONDITION>");
            string childIndent = indent + (isLast ? "   " : "|  ");
            sb.AppendLine($"{childIndent}├─ '(': \"{OpenParen}\"");
            if (Expr != null) sb.Append(Expr.Print(childIndent, false));
            sb.AppendLine($"{childIndent}└─ ')': \"{CloseParen}\"");
            return sb.ToString();
        }
    }

    public class ExprNode : Node
    {
        public List<LogicTermNode> LogicTerms { get; set; } = new List<LogicTermNode>();
        public List<string> Ops { get; set; } = new List<string>();

        public override string Print(string indent, bool isLast)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{indent}{(isLast ? "└─ " : "├─ ")}<EXPR>");
            string childIndent = indent + (isLast ? "   " : "|  ");
            for (int i = 0; i < LogicTerms.Count; i++)
            {
                bool isCurrentLast = (i == LogicTerms.Count - 1 && Ops.Count <= i);
                sb.Append(LogicTerms[i].Print(childIndent, isCurrentLast));
                if (i < Ops.Count)
                {
                    bool opLast = (i == LogicTerms.Count - 1 && i == Ops.Count - 1);
                    sb.AppendLine($"{childIndent}{(opLast ? "└─ " : "├─ ")}'||': \"{Ops[i]}\"");
                }
            }
            return sb.ToString();
        }
    }

    public class LogicTermNode : Node
    {
        public List<CompareNode> Compares { get; set; } = new List<CompareNode>();
        public List<string> Ops { get; set; } = new List<string>();

        public override string Print(string indent, bool isLast)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{indent}{(isLast ? "└─ " : "├─ ")}<LOGIC_TERM>");
            string childIndent = indent + (isLast ? "   " : "|  ");
            for (int i = 0; i < Compares.Count; i++)
            {
                bool isCurrentLast = (i == Compares.Count - 1 && Ops.Count <= i);
                sb.Append(Compares[i].Print(childIndent, isCurrentLast));
                if (i < Ops.Count)
                {
                    bool opLast = (i == Compares.Count - 1 && i == Ops.Count - 1);
                    sb.AppendLine($"{childIndent}{(opLast ? "└─ " : "├─ ")}'&&': \"{Ops[i]}\"");
                }
            }
            return sb.ToString();
        }
    }

    public class CompareNode : Node
    {
        public ValueNode LeftValue { get; set; }
        public string RelOp { get; set; }
        public ValueNode RightValue { get; set; }
        public ExprNode InnerExpr { get; set; }

        public override string Print(string indent, bool isLast)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{indent}{(isLast ? "└─ " : "├─ ")}<COMPARE>");
            string childIndent = indent + (isLast ? "   " : "|  ");
            if (InnerExpr != null)
            {
                sb.Append(InnerExpr.Print(childIndent, true));
            }
            else
            {
                if (LeftValue != null) sb.Append(LeftValue.Print(childIndent, false));
                sb.AppendLine($"{childIndent}├─ <REL_OP>: \"{RelOp}\"");
                if (RightValue != null) sb.Append(RightValue.Print(childIndent, true));
            }
            return sb.ToString();
        }
    }

    public class ValueNode : Node
    {
        public string Name { get; set; }

        public override string Print(string indent, bool isLast)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{indent}{(isLast ? "└─ " : "├─ ")}<VALUE>: \"{Name}\"");
            return sb.ToString();
        }
    }

    public class BlockNode : Node
    {
        public string OpenBrace { get; set; }
        public StatementListNode StatementList { get; set; }
        public string CloseBrace { get; set; }

        public override string Print(string indent, bool isLast)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{indent}{(isLast ? "└─ " : "├─ ")}<BLOCK>");
            string childIndent = indent + (isLast ? "   " : "|  ");
            sb.AppendLine($"{childIndent}├─ '{{': \"{OpenBrace}\"");
            if (StatementList != null) sb.Append(StatementList.Print(childIndent, false));
            sb.AppendLine($"{childIndent}└─ '}}': \"{CloseBrace}\"");
            return sb.ToString();
        }
    }

    public class StatementListNode : Node
    {
        public List<StatementNode> Statements { get; set; } = new List<StatementNode>();

        public override string Print(string indent, bool isLast)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{indent}{(isLast ? "└─ " : "├─ ")}<STATEMENT_LIST>");
            string childIndent = indent + (isLast ? "   " : "|  ");
            for (int i = 0; i < Statements.Count; i++)
            {
                sb.Append(Statements[i].Print(childIndent, i == Statements.Count - 1));
            }
            return sb.ToString();
        }
    }

    public class StatementNode : Node
    {
        public ValueNode Target { get; set; }
        public string Op { get; set; }
        public string VarName { get; set; }
        public string Semicolon { get; set; }

        public override string Print(string indent, bool isLast)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{indent}{(isLast ? "└─ " : "├─ ")}<STATEMENT>");
            string childIndent = indent + (isLast ? "   " : "|  ");
            if (Target != null) sb.Append(Target.Print(childIndent, false));
            sb.AppendLine($"{childIndent}├─ <OP>: \"{Op}\"");
            sb.AppendLine($"{childIndent}├─ <VAR>: \"{VarName}\"");
            sb.AppendLine($"{childIndent}└─ ';': \"{Semicolon}\"");
            return sb.ToString();
        }
    }

    public class ElsePartNode : Node
    {
        public string ElseToken { get; set; }
        public BlockNode ElseBlock { get; set; }

        public override string Print(string indent, bool isLast)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{indent}{(isLast ? "└─ " : "├─ ")}<ELSE_PART>");
            string childIndent = indent + (isLast ? "   " : "|  ");
            sb.AppendLine($"{childIndent}├─ 'else': \"{ElseToken}\"");
            if (ElseBlock != null) sb.Append(ElseBlock.Print(childIndent, true));
            return sb.ToString();
        }
    }

    public class AstBuilder
    {
        private List<ScanToken> _tokens;
        private int _pos;
        private ScanToken _curr;
        private List<AnalysisError> _errors;

        public AstBuilder(List<ScanToken> tokens)
        {
            _tokens = tokens;
            _pos = 0;
            _errors = new List<AnalysisError>();
            _curr = _tokens.Count > 0 ? _tokens[0] : null;
        }

        private void Next()
        {
            _pos++;
            _curr = _pos < _tokens.Count ? _tokens[_pos] : null;
        }

        private bool IsCode(int code)
        {
            return _curr != null && _curr.Usl_code == code;
        }

        private bool IsLecsema(string lecsema)
        {
            return _curr != null && _curr.Lecsema == lecsema;
        }

        private ScanToken Match(int code)
        {
            if (IsCode(code))
            {
                ScanToken t = _curr;
                Next();
                return t;
            }
            return null;
        }

        private ScanToken MatchLecsema(string lecsema)
        {
            if (IsLecsema(lecsema))
            {
                ScanToken t = _curr;
                Next();
                return t;
            }
            return null;
        }

        public SemanticResult Build()
        {
            SemanticResult res = new SemanticResult();
            StartNode root = ParseStart();
            res.Errors = _errors;
            if (root != null)
            {
                res.AstText = root.Print("", true);
                PerformSemanticCheck(root);
            }
            return res;
        }

        private StartNode ParseStart()
        {
            StartNode node = new StartNode();
            ScanToken tIf = Match(2);
            if (tIf != null)
            {
                node.IfToken = tIf.Lecsema;
            }
            node.Condition = ParseCondition();
            node.IfBlock = ParseBlock();
            if (IsCode(3))
            {
                node.ElsePart = ParseElsePart();
            }
            ScanToken tSemi = Match(17);
            if (tSemi != null)
            {
                node.EndSemi = tSemi.Lecsema;
            }
            return node;
        }

        private ConditionNode ParseCondition()
        {
            ConditionNode node = new ConditionNode();
            ScanToken open = Match(11);
            node.OpenParen = open != null ? open.Lecsema : "(";
            node.Expr = ParseExpr();
            ScanToken close = Match(12);
            node.CloseParen = close != null ? close.Lecsema : ")";
            return node;
        }

        private ExprNode ParseExpr()
        {
            ExprNode node = new ExprNode();
            LogicTermNode term = ParseLogicTerm();
            if (term != null) node.LogicTerms.Add(term);
            while (IsLecsema("||"))
            {
                ScanToken op = MatchLecsema("||");
                node.Ops.Add(op.Lecsema);
                LogicTermNode nextTerm = ParseLogicTerm();
                if (nextTerm != null) node.LogicTerms.Add(nextTerm);
            }
            return node;
        }

        private LogicTermNode ParseLogicTerm()
        {
            LogicTermNode node = new LogicTermNode();
            CompareNode comp = ParseCompare();
            if (comp != null) node.Compares.Add(comp);
            while (IsLecsema("&&"))
            {
                ScanToken op = MatchLecsema("&&");
                node.Ops.Add(op.Lecsema);
                CompareNode nextComp = ParseCompare();
                if (nextComp != null) node.Compares.Add(nextComp);
            }
            return node;
        }

        private CompareNode ParseCompare()
        {
            CompareNode node = new CompareNode();
            if (IsCode(11))
            {
                Next();
                node.InnerExpr = ParseExpr();
                Match(12);
                return node;
            }
            node.LeftValue = ParseValue();
            int[] relCodes = { 13, 14, 15, 16, 19, 20 };
            if (_curr != null && relCodes.Contains(_curr.Usl_code))
            {
                node.RelOp = _curr.Lecsema;
                Next();
            }
            node.RightValue = ParseValue();
            return node;
        }

        private ValueNode ParseValue()
        {
            if (IsCode(1))
            {
                ScanToken t = Match(1);
                return new ValueNode { Name = t.Lecsema };
            }
            return null;
        }

        private BlockNode ParseBlock()
        {
            BlockNode node = new BlockNode();
            ScanToken open = Match(9);
            node.OpenBrace = open != null ? open.Lecsema : "{";
            node.StatementList = ParseStatementList();
            ScanToken close = Match(10);
            node.CloseBrace = close != null ? close.Lecsema : "}";
            return node;
        }

        private StatementListNode ParseStatementList()
        {
            StatementListNode node = new StatementListNode();
            StatementNode stmt = ParseStatement();
            if (stmt != null) node.Statements.Add(stmt);
            while (IsCode(1))
            {
                StatementNode nextStmt = ParseStatement();
                if (nextStmt != null) node.Statements.Add(nextStmt);
            }
            return node;
        }

        private StatementNode ParseStatement()
        {
            if (!IsCode(1)) return null;
            StatementNode node = new StatementNode();
            node.Target = ParseValue();
            if (IsCode(18) || IsLecsema("++") || IsLecsema("--"))
            {
                node.Op = _curr.Lecsema;
                Next();
            }
            if (IsCode(1))
            {
                ScanToken v = Match(1);
                node.VarName = v.Lecsema;
            }
            else
            {
                node.VarName = "";
            }
            ScanToken semi = Match(17);
            node.Semicolon = semi != null ? semi.Lecsema : ";";
            return node;
        }

        private ElsePartNode ParseElsePart()
        {
            ElsePartNode node = new ElsePartNode();
            ScanToken tElse = Match(3);
            node.ElseToken = tElse != null ? tElse.Lecsema : "else";
            node.ElseBlock = ParseBlock();
            return node;
        }

        private void PerformSemanticCheck(StartNode root)
        {
            if (root.Condition?.Expr == null) return;
            HashSet<string> condVars = new HashSet<string>();
            ExtractVarsFromExpr(root.Condition.Expr, condVars);
            if (root.IfBlock?.StatementList != null)
            {
                foreach (var stmt in root.IfBlock.StatementList.Statements)
                {
                    if (stmt.Target != null && !condVars.Contains(stmt.Target.Name) && !string.IsNullOrEmpty(stmt.VarName) && !condVars.Contains(stmt.VarName))
                    {
                        _errors.Add(new AnalysisError("Семантическая", $"{stmt.Target.Name} {stmt.Op} {stmt.VarName}", "", "Ошибка: в блоке if должен быть идентификатор из условия."));
                    }
                }
            }
            if (root.ElsePart?.ElseBlock?.StatementList != null)
            {
                foreach (var stmt in root.ElsePart.ElseBlock.StatementList.Statements)
                {
                    if (stmt.Target != null && !condVars.Contains(stmt.Target.Name) && !string.IsNullOrEmpty(stmt.VarName) && !condVars.Contains(stmt.VarName))
                    {
                        _errors.Add(new AnalysisError("Семантическая", $"{stmt.Target.Name} {stmt.Op} {stmt.VarName}", "", "Ошибка: в блоке else должен быть идентификатор из условия."));
                    }
                }
            }
        }

        private void ExtractVarsFromExpr(ExprNode expr, HashSet<string> vars)
        {
            if (expr == null) return;
            foreach (var term in expr.LogicTerms)
            {
                foreach (var comp in term.Compares)
                {
                    if (comp.InnerExpr != null)
                    {
                        ExtractVarsFromExpr(comp.InnerExpr, vars);
                    }
                    else
                    {
                        if (comp.LeftValue != null) vars.Add(comp.LeftValue.Name);
                        if (comp.RightValue != null) vars.Add(comp.RightValue.Name);
                    }
                }
            }
        }
    }
}
