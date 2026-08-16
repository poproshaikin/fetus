using Compiler.AST;
using Compiler.IR;
using Compiler.Semantic;

namespace Compiler.Lowering;

public class Lowerer
{
    public Lowerer(BoundProgram program)
    {
        _program = program;
    }

    public Module Lower()
    {
        var topLevel = new List<BoundStatement>();

        foreach (var stmt in _program.Body)
        {
            if (stmt is BoundFuncDecl funcDecl) 
                LowerFuncDecl(funcDecl);
            else
                topLevel.Add(stmt);
        }
        
        _builder.EnterFunction();
        foreach (var stmt in topLevel)
            LowerStatement(stmt);
        _builder.EndFunction("main", IntType.Int32);

        return _builder.Build();
    }

    private readonly BoundProgram _program;
    private readonly ModuleBuilder _builder = new();
    private Scope _scope = new();

    private void LowerStatement(BoundStatement stmt)
    {
        switch (stmt)
        {
            case BoundVarDecl varDecl: LowerVarDecl(varDecl); break;
            case BoundExprStatement exprStmt: LowerExpression(exprStmt.Expression); break;
            case BoundIf @if: LowerIf(@if); break;
            case BoundWhile @while: LowerWhile(@while); break;
            case BoundReturn @return: LowerReturn(@return); break;
        }
    }

    private void LowerReturn(BoundReturn @return)
    {
        var value = @return.Value != null ? LowerExpression(@return.Value) : null;
        _builder.Return(value);
    }
    
    // while (1 > 2) { someStmt }
    
    // startCycle
    // cmp 1, 2
    // jle end
    //     someStmt
    //     jmp startCycle
    //
    // end:

    private void LowerWhile(BoundWhile @while)
    {
        var startCycle = _builder.NewLabel();
        var endCycle = _builder.NewLabel();
        
        _builder.MarkLabel(startCycle);
        var condition = LowerExpression(@while.Condition);
        _builder.JumpIfFalse(condition, endCycle);
        LowerBlock(@while.Body);
        _builder.Jump(startCycle);
        _builder.MarkLabel(endCycle);
    }
    
    // if (1 > 2) { someStmt }
    // else if (2 > 3) { someSecondStmt } 
    // else
    
    // cmp 1, 2
    // jle nextIf
    //     someStmt
    //     jmp end
    // nextIf:
    // 
    // cmp 2, 3
    // jle commonElse
    //     someSecondStmt
    //     jmp end
    //
    // commonElse:
    //     someStmt
    //     jmp end
    //
    // end:
    // 

    private void LowerIf(BoundIf @if, Label? endLabel = null)
    {
        var outerIf = endLabel == null;
        var end = endLabel ?? _builder.NewLabel();
        var next = _builder.NewLabel();
        var condition = LowerExpression(@if.Condition);

        _builder.JumpIfFalse(condition, next);
        LowerBlock(@if.Then);
        _builder.Jump(end);

        _builder.MarkLabel(next);

        if (@if.Else is BoundIf elseIf)
        {
            LowerIf(elseIf, end);
        }
        else if (@if.Else is BoundBlock elseBlock)
        {
            LowerBlock(elseBlock);
        }

        if (outerIf)
            _builder.MarkLabel(end);
    }

    private void LowerFuncDecl(BoundFuncDecl funcDecl)
    {
        _builder.EnterFunction();
        var savedScope = _scope;
        _scope = new Scope();

        for (int i = 0; i < funcDecl.Params.Count; i++)
        {
            var p = funcDecl.Params[i];
            var entry = _builder.PushParam(p.Name, p.Type, i);
            _scope.Declare(entry);
        }
        
        LowerBlock(funcDecl.Body);
        _builder.EndFunction(funcDecl.Name, funcDecl.ReturnType);
        _scope = savedScope;
    }

    private void LowerBlock(BoundBlock block)
    {
        var savedScope = _scope;
        _scope = savedScope.CreateChild();
        
        foreach (var stmt in block.Body)
            LowerStatement(stmt);
        
        _scope = savedScope;
    }

    private void LowerVarDecl(BoundVarDecl varDecl)
    {
        var entry = _builder.Push(varDecl.Name, varDecl.Type);
        _scope.Declare(entry);

        if (varDecl.Init != null)
        {
            var value = LowerExpression(varDecl.Init);
            _builder.Mov(entry, value);
        }
    }

    private StackEntry LowerExpression(BoundExpression expr)
    {
        return expr switch
        {
            BoundAssignment assignment => LowerAssignment(assignment),
            BoundBinary binary => LowerBinary(binary),
            BoundCall call => LowerCall(call),
            BoundSyscall syscall => LowerSyscall(syscall),
            BoundPeek peek => LowerPeek(peek),
            BoundIdentifier identifier => LowerIdentifier(identifier),
            BoundLiteral literal => LowerLiteral(literal),
            BoundCast cast => LowerCast(cast),
            _ => throw new ArgumentOutOfRangeException(nameof(expr), expr, null)
        };
    }

    private StackEntry LowerCast(BoundCast cast)
    {
        var value = LowerExpression(cast.Value);
        var dest = _builder.PushAnon(cast.Type);
        _builder.Mov(dest, value);
        return dest;
    }

    private StackEntry LowerIdentifier(BoundIdentifier identifier) => _scope.Resolve(identifier.Name)!;

    private StackEntry LowerLiteral(BoundLiteral literal)
    {
        if (literal.Type is FloatType)
            throw new NotImplementedException($"Lowering '{literal.Type.TypeName}' literals is not implemented yet");

        if (literal.Value is string str)
            return _builder.LoadConstString(literal.Type, str);

        var value = literal.Value switch
        {
            bool b => b ? 1L : 0L,
            double d => (long)d,
            _ => throw new NotImplementedException($"Lowering literal value '{literal.Value}' is not implemented yet"),
        };

        return _builder.LoadConst(literal.Type, value);
    }

    private StackEntry LowerBinary(BoundBinary binary)
    {
        if (binary.Op == BinaryOperator.And)
            return LowerAnd(binary);

        if (binary.Op == BinaryOperator.Or)
            return LowerOr(binary);

        var left = LowerExpression(binary.Left);
        var right = LowerExpression(binary.Right);

        return binary.Op switch
        {
            BinaryOperator.Plus => _builder.Add(binary.Type, left, right),
            BinaryOperator.Minus => _builder.Sub(binary.Type, left, right),
            BinaryOperator.Star => _builder.Mul(binary.Type, left, right),
            BinaryOperator.Slash => _builder.Div(binary.Type, left, right),
            BinaryOperator.Lt => _builder.CmpLt(left, right),
            BinaryOperator.Gt => _builder.CmpGt(left, right),
            BinaryOperator.LtEq => _builder.CmpLtEq(left, right),
            BinaryOperator.GtEq => _builder.CmpGtEq(left, right),
            BinaryOperator.Eq => _builder.CmpEq(left, right),
            BinaryOperator.NotEq => _builder.CmpNotEq(left, right),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    // a && b 
    private StackEntry LowerAnd(BoundBinary binary)
    {
        var result = _builder.PushAnon(binary.Type);
        var end = _builder.NewLabel();

        var left = LowerExpression(binary.Left);
        _builder.Mov(result, left);
        _builder.JumpIfFalse(left, end);

        var right = LowerExpression(binary.Right);
        _builder.Mov(result, right);

        _builder.MarkLabel(end);
        return result;
    }

    // a || b
    private StackEntry LowerOr(BoundBinary binary)
    {
        var result = _builder.PushAnon(binary.Type);
        var end = _builder.NewLabel();

        var left = LowerExpression(binary.Left);
        _builder.Mov(result, left);
        _builder.JumpIfTrue(left, end);

        var right = LowerExpression(binary.Right);
        _builder.Mov(result, right);

        _builder.MarkLabel(end);
        return result;
    }

    private StackEntry LowerAssignment(BoundAssignment assignment)
    {
        var value = LowerExpression(assignment.Value);
        var target = _scope.Resolve(assignment.Target)!;
        _builder.Mov(target, value);
        return value;
    }

    private StackEntry LowerCall(BoundCall call)
    {
        foreach (var p in call.Args.Select(LowerExpression).Reverse())
            _builder.SetParam(p);

        return _builder.Call(call.Callee, call.Type, call.Args.Count)!;
    }

    private StackEntry LowerSyscall(BoundSyscall syscall)
    {
        var args = syscall.Args.Select(LowerExpression).ToList();
        return _builder.Syscall(syscall.Type, args);
    }
    
    private StackEntry LowerPeek(BoundPeek peek)
    {
        return _builder.Peek(LowerExpression(peek.Address), LowerExpression(peek.Offset));
    }

}