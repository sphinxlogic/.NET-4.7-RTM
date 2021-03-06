/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Apache License, Version 2.0, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Runtime.CompilerServices;

#if SILVERLIGHT
using System.Core;
#endif

#if CLR2
namespace Microsoft.Scripting.Ast {
#else
namespace System.Linq.Expressions {
#endif
    using Compiler;

    /// <summary>
    /// Creates a <see cref="LambdaExpression"/> node.
    /// This captures a block of code that is similar to a .NET method body.
    /// </summary>
    /// <remarks>
    /// Lambda expressions take input through parameters and are expected to be fully bound. 
    /// </remarks>
#if !SILVERLIGHT
    [DebuggerTypeProxy(typeof(Expression.LambdaExpressionProxy))]
#endif
    public abstract class LambdaExpression : Expression {
        private readonly string _name;
        private readonly Expression _body;
        private readonly ReadOnlyCollection<ParameterExpression> _parameters;
        private readonly Type _delegateType;
        private readonly bool _tailCall;

        internal LambdaExpression(
            Type delegateType,
            string name,
            Expression body,
            bool tailCall,
            ReadOnlyCollection<ParameterExpression> parameters
        ) {

            Debug.Assert(delegateType != null);

            _name = name;
            _body = body;
            _parameters = parameters;
            _delegateType = delegateType;
            _tailCall = tailCall;
        }

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression" /> represents. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="Type"/> that represents the static type of the expression.</returns>
        public sealed override Type Type {
            get { return _delegateType; }
        }

        /// <summary>
        /// Returns the node type of this <see cref="Expression" />. (Inherited from <see cref="Expression" />.)
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> that represents this expression.</returns>
        public sealed override ExpressionType NodeType {
            get { return ExpressionType.Lambda; }
        }

        /// <summary>
        /// Gets the parameters of the lambda expression. 
        /// </summary>
        public ReadOnlyCollection<ParameterExpression> Parameters {
            get { return _parameters; }
        }

        /// <summary>
        /// Gets the name of the lambda expression. 
        /// </summary>
        /// <remarks>Used for debugging purposes.</remarks>
        public string Name {
            get { return _name; }
        }

        /// <summary>
        /// Gets the body of the lambda expression. 
        /// </summary>
        public Expression Body {
            get { return _body; }
        }

        /// <summary>
        /// Gets the return type of the lambda expression. 
        /// </summary>
        public Type ReturnType {
            get { return Type.GetMethod("Invoke").ReturnType; }
        }

        /// <summary>
        /// Gets the value that indicates if the lambda expression will be compiled with
        /// tail call optimization. 
        /// </summary>
        public bool TailCall {
            get { return _tailCall; }
        }

        /// <summary>
        /// Produces a delegate that represents the lambda expression.
        /// </summary>
        /// <returns>A delegate containing the compiled version of the lambda.</returns>
        public Delegate Compile() {
            return LambdaCompiler.Compile(this, null);
        }

        /// <summary>
        /// Produces a delegate that represents the lambda expression.
        /// </summary>
        /// <param name="debugInfoGenerator">Debugging information generator used by the compiler to mark sequence points and annotate local variables.</param>
        /// <returns>A delegate containing the compiled version of the lambda.</returns>
        public Delegate Compile(DebugInfoGenerator debugInfoGenerator) {
            ContractUtils.RequiresNotNull(debugInfoGenerator, "debugInfoGenerator");
            return LambdaCompiler.Compile(this, debugInfoGenerator);
        }

        /// <summary>
        /// Produces a delegate that represents the lambda expression.
        /// </summary>
        /// <param name="preferInterpretation">A <see cref="bool"/> that indicates if the expression should be compiled to an interpreted form, if available.</param>
        /// <returns>A delegate containing the compiled version of the lambda.</returns>
        public Delegate Compile(bool preferInterpretation)
        {
            return Compile();
        }

        /// <summary>
        /// Compiles the lambda into a method definition.
        /// </summary>
        /// <param name="method">A <see cref="MethodBuilder"/> which will be used to hold the lambda's IL.</param>
        public void CompileToMethod(MethodBuilder method) {
            CompileToMethodInternal(method, null);
        }

        /// <summary>
        /// Compiles the lambda into a method definition and custom debug information.
        /// </summary>
        /// <param name="method">A <see cref="MethodBuilder"/> which will be used to hold the lambda's IL.</param>
        /// <param name="debugInfoGenerator">Debugging information generator used by the compiler to mark sequence points and annotate local variables.</param>
        public void CompileToMethod(MethodBuilder method, DebugInfoGenerator debugInfoGenerator) {
            ContractUtils.RequiresNotNull(debugInfoGenerator, "debugInfoGenerator");
            CompileToMethodInternal(method, debugInfoGenerator);
        }

        private void CompileToMethodInternal(MethodBuilder method, DebugInfoGenerator debugInfoGenerator) {
            ContractUtils.RequiresNotNull(method, "method");
            ContractUtils.Requires(method.IsStatic, "method");
            var type = method.DeclaringType as TypeBuilder;
            if (type == null) throw Error.MethodBuilderDoesNotHaveTypeBuilder();

            LambdaCompiler.Compile(this, method, debugInfoGenerator);
        }

        internal abstract LambdaExpression Accept(StackSpiller spiller);
    }

    /// <summary>
    /// Defines a <see cref="Expression{TDelegate}"/> node.
    /// This captures a block of code that is similar to a .NET method body.
    /// </summary>
    /// <typeparam name="TDelegate">The type of the delegate.</typeparam>
    /// <remarks>
    /// Lambda expressions take input through parameters and are expected to be fully bound. 
    /// </remarks>
    public sealed class Expression<TDelegate> : LambdaExpression {
        internal Expression(Expression body, string name, bool tailCall, ReadOnlyCollection<ParameterExpression> parameters)
            : base(typeof(TDelegate), name, body, tailCall, parameters) {
        }

        /// <summary>
        /// Produces a delegate that represents the lambda expression.
        /// </summary>
        /// <returns>A delegate containing the compiled version of the lambda.</returns>
        public new TDelegate Compile() {
            return (TDelegate)(object)LambdaCompiler.Compile(this, null);
        }

        /// <summary>
        /// Produces a delegate that represents the lambda expression.
        /// </summary>
        /// <param name="debugInfoGenerator">Debugging information generator used by the compiler to mark sequence points and annotate local variables.</param>
        /// <returns>A delegate containing the compiled version of the lambda.</returns>
        public new TDelegate Compile(DebugInfoGenerator debugInfoGenerator) {
            ContractUtils.RequiresNotNull(debugInfoGenerator, "debugInfoGenerator");
            return (TDelegate)(object)LambdaCompiler.Compile(this, debugInfoGenerator);
        }

        public new TDelegate Compile(bool preferInterpretation)
        {
            return Compile();
        }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="body">The <see cref="LambdaExpression.Body">Body</see> property of the result.</param>
        /// <param name="parameters">The <see cref="LambdaExpression.Parameters">Parameters</see> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public Expression<TDelegate> Update(Expression body, IEnumerable<ParameterExpression> parameters) {
            if (body == Body && parameters == Parameters) {
                return this;
            }
            return Expression.Lambda<TDelegate>(body, Name, TailCall, parameters);
        }

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
        protected internal override Expression Accept(ExpressionVisitor visitor) {
            return visitor.VisitLambda(this);
        }

        internal override LambdaExpression Accept(StackSpiller spiller) {
            return spiller.Rewrite(this);
        }

        internal static LambdaExpression Create(Expression body, string name, bool tailCall, ReadOnlyCollection<ParameterExpression> parameters) {
            return new Expression<TDelegate>(body, name, tailCall, parameters);
        }
    }


    public partial class Expression {

        /// <summary>
        /// Creates an Expression{T} given the delegate type. Caches the
        /// factory method to speed up repeated creations for the same T.
        /// </summary>
        internal static LambdaExpression CreateLambda(Type delegateType, Expression body, string name, bool tailCall, ReadOnlyCollection<ParameterExpression> parameters) {
            // Get or create a delegate to the public Expression.Lambda<T>
            // method and call that will be used for creating instances of this
            // delegate type
            LambdaFactory fastPath;
            var factories = _LambdaFactories;
            if (factories == null) {
                _LambdaFactories = factories = new CacheDict<Type, LambdaFactory>(50);
            }

            MethodInfo create = null;
            if (!factories.TryGetValue(delegateType, out fastPath)) {
                create = typeof(Expression<>).MakeGenericType(delegateType).GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic);
                if (TypeUtils.CanCache(delegateType)) {
                    factories[delegateType] = fastPath = (LambdaFactory)Delegate.CreateDelegate(typeof(LambdaFactory), create);
                }
            }

            if (fastPath != null) {
                return fastPath(body, name, tailCall, parameters);
            }
            
            Debug.Assert(create != null);
            return (LambdaExpression)create.Invoke(null, new object[] { body, name, tailCall, parameters });
        }

        /// <summary>
        /// Creates an <see cref="Expression{TDelegate}"/> where the delegate type is known at compile time. 
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type. </typeparam>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="P:Body"/> property equal to. </param>
        /// <param name="parameters">An array that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="P:Parameters"/> collection. </param>
        /// <returns>An <see cref="Expression{TDelegate}"/> that has the <see cref="P:NodeType"/> property equal to <see cref="P:Lambda"/> and the <see cref="P:Body"/> and <see cref="P:Parameters"/> properties set to the specified values.</returns>
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, params ParameterExpression[] parameters) {
            return Lambda<TDelegate>(body, false, (IEnumerable<ParameterExpression>)parameters);
        }

        /// <summary>
        /// Creates an <see cref="Expression{TDelegate}"/> where the delegate type is known at compile time. 
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type. </typeparam>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="P:Body"/> property equal to. </param>
        /// <param name="tailCall">A <see cref="Boolean"/> that indicates if tail call optimization will be applied when compiling the created expression. </param>
        /// <param name="parameters">An array that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="P:Parameters"/> collection. </param>
        /// <returns>An <see cref="Expression{TDelegate}"/> that has the <see cref="P:NodeType"/> property equal to <see cref="P:Lambda"/> and the <see cref="P:Body"/> and <see cref="P:Parameters"/> properties set to the specified values.</returns>
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, bool tailCall, params ParameterExpression[] parameters) {
            return Lambda<TDelegate>(body, tailCall, (IEnumerable<ParameterExpression>)parameters);
        }

        /// <summary>
        /// Creates an <see cref="Expression{TDelegate}"/> where the delegate type is known at compile time. 
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type. </typeparam>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="P:Body"/> property equal to. </param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="P:Parameters"/> collection. </param>
        /// <returns>An <see cref="Expression{TDelegate}"/> that has the <see cref="P:NodeType"/> property equal to <see cref="P:Lambda"/> and the <see cref="P:Body"/> and <see cref="P:Parameters"/> properties set to the specified values.</returns>
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, IEnumerable<ParameterExpression> parameters) {
            return Lambda<TDelegate>(body, null, false, parameters);
        }

        /// <summary>
        /// Creates an <see cref="Expression{TDelegate}"/> where the delegate type is known at compile time. 
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type. </typeparam>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="P:Body"/> property equal to. </param>
        /// <param name="tailCall">A <see cref="Boolean"/> that indicates if tail call optimization will be applied when compiling the created expression. </param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="P:Parameters"/> collection. </param>
        /// <returns>An <see cref="Expression{TDelegate}"/> that has the <see cref="P:NodeType"/> property equal to <see cref="P:Lambda"/> and the <see cref="P:Body"/> and <see cref="P:Parameters"/> properties set to the specified values.</returns>
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, bool tailCall, IEnumerable<ParameterExpression> parameters) {
            return Lambda<TDelegate>(body, null, tailCall, parameters);
        }

        /// <summary>
        /// Creates an <see cref="Expression{TDelegate}"/> where the delegate type is known at compile time. 
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type. </typeparam>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="P:Body"/> property equal to. </param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="P:Parameters"/> collection. </param>
        /// <param name="name">The name of the lambda. Used for generating debugging info.</param>
        /// <returns>An <see cref="Expression{TDelegate}"/> that has the <see cref="P:NodeType"/> property equal to <see cref="P:Lambda"/> and the <see cref="P:Body"/> and <see cref="P:Parameters"/> properties set to the specified values.</returns>
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, String name, IEnumerable<ParameterExpression> parameters) {
            return Lambda<TDelegate>(body, name, false, parameters);
        }

        /// <summary>
        /// Creates an <see cref="Expression{TDelegate}"/> where the delegate type is known at compile time. 
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type. </typeparam>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="P:Body"/> property equal to. </param>
        /// <param name="name">The name of the lambda. Used for generating debugging info.</param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="P:Parameters"/> collection. </param>
        /// <param name="tailCall">A <see cref="Boolean"/> that indicates if tail call optimization will be applied when compiling the created expression. </param>
        /// <returns>An <see cref="Expression{TDelegate}"/> that has the <see cref="P:NodeType"/> property equal to <see cref="P:Lambda"/> and the <see cref="P:Body"/> and <see cref="P:Parameters"/> properties set to the specified values.</returns>
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, String name, bool tailCall, IEnumerable<ParameterExpression> parameters) {
            var parameterList = parameters.ToReadOnly();
            ValidateLambdaArgs(typeof(TDelegate), ref body, parameterList);
            return new Expression<TDelegate>(body, name, tailCall, parameterList);
        }


        /// <summary>
        /// Creates a LambdaExpression by first constructing a delegate type. 
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="P:Body"/> property equal to. </param>
        /// <param name="parameters">An array that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="P:Parameters"/> collection. </param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="P:NodeType"/> property equal to Lambda and the <see cref="P:Body"/> and <see cref="P:Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Expression body, params ParameterExpression[] parameters) {
            return Lambda(body, false, (IEnumerable<ParameterExpression>)parameters);
        }

        /// <summary>
        /// Creates a LambdaExpression by first constructing a delegate type. 
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="P:Body"/> property equal to. </param>
        /// <param name="tailCall">A <see cref="Boolean"/> that indicates if tail call optimization will be applied when compiling the created expression. </param>
        /// <param name="parameters">An array that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="P:Parameters"/> collection. </param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="P:NodeType"/> property equal to Lambda and the <see cref="P:Body"/> and <see cref="P:Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Expression body, bool tailCall, params ParameterExpression[] parameters) {
            return Lambda(body, tailCall, (IEnumerable<ParameterExpression>)parameters);
        }

        /// <summary>
        /// Creates a LambdaExpression by first constructing a delegate type. 
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="P:Body"/> property equal to. </param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="P:Parameters"/> collection. </param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="P:NodeType"/> property equal to Lambda and the <see cref="P:Body"/> and <see cref="P:Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Expression body, IEnumerable<ParameterExpression> parameters) {
            return Lambda(body, null, false, parameters);
        }

        /// <summary>
        /// Creates a LambdaExpression by first constructing a delegate type. 
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="P:Body"/> property equal to. </param>
        /// <param name="tailCall">A <see cref="Boolean"/> that indicates if tail call optimization will be applied when compiling the created expression. </param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="P:Parameters"/> collection. </param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="P:NodeType"/> property equal to Lambda and the <see cref="P:Body"/> and <see cref="P:Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Expression body, bool tailCall, IEnumerable<ParameterExpression> parameters) {
            return Lambda(body, null, tailCall, parameters);
        }

        /// <summary>
        /// Creates a LambdaExpression by first constructing a delegate type. 
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="P:Body"/> property equal to. </param>
        /// <param name="parameters">An array that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="P:Parameters"/> collection. </param>
        /// <param name="delegateType">A <see cref="Type"/> representing the delegate signature for the lambda.</param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="P:NodeType"/> property equal to Lambda and the <see cref="P:Body"/> and <see cref="P:Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Type delegateType, Expression body, params ParameterExpression[] parameters) {
            return Lambda(delegateType, body, null, false, parameters);
        }

        /// <summary>
        /// Creates a LambdaExpression by first constructing a delegate type. 
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="P:Body"/> property equal to. </param>
        /// <param name="tailCall">A <see cref="Boolean"/> that indicates if tail call optimization will be applied when compiling the created expression. </param>
        /// <param name="parameters">An array that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="P:Parameters"/> collection. </param>
        /// <param name="delegateType">A <see cref="Type"/> representing the delegate signature for the lambda.</param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="P:NodeType"/> property equal to Lambda and the <see cref="P:Body"/> and <see cref="P:Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Type delegateType, Expression body, bool tailCall, params ParameterExpression[] parameters) {
            return Lambda(delegateType, body, null, tailCall, parameters);
        }

        /// <summary>
        /// Creates a LambdaExpression by first constructing a delegate type. 
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="P:Body"/> property equal to. </param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="P:Parameters"/> collection. </param>
        /// <param name="delegateType">A <see cref="Type"/> representing the delegate signature for the lambda.</param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="P:NodeType"/> property equal to Lambda and the <see cref="P:Body"/> and <see cref="P:Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Type delegateType, Expression body, IEnumerable<ParameterExpression> parameters) {
            return Lambda(delegateType, body, null, false, parameters);
        }

        /// <summary>
        /// Creates a LambdaExpression by first constructing a delegate type. 
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="P:Body"/> property equal to. </param>
        /// <param name="tailCall">A <see cref="Boolean"/> that indicates if tail call optimization will be applied when compiling the created expression. </param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="P:Parameters"/> collection. </param>
        /// <param name="delegateType">A <see cref="Type"/> representing the delegate signature for the lambda.</param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="P:NodeType"/> property equal to Lambda and the <see cref="P:Body"/> and <see cref="P:Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Type delegateType, Expression body, bool tailCall, IEnumerable<ParameterExpression> parameters) {
            return Lambda(delegateType, body, null, tailCall, parameters);
        }

        /// <summary>
        /// Creates a LambdaExpression by first constructing a delegate type. 
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="P:Body"/> property equal to. </param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="P:Parameters"/> collection. </param>
        /// <param name="name">The name for the lambda. Used for emitting debug information.</param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="P:NodeType"/> property equal to Lambda and the <see cref="P:Body"/> and <see cref="P:Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Expression body, string name, IEnumerable<ParameterExpression> parameters) {
            return Lambda(body, name, false, parameters);
        }

        /// <summary>
        /// Creates a LambdaExpression by first constructing a delegate type. 
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="P:Body"/> property equal to. </param>
        /// <param name="name">The name for the lambda. Used for emitting debug information.</param>
        /// <param name="tailCall">A <see cref="Boolean"/> that indicates if tail call optimization will be applied when compiling the created expression. </param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="P:Parameters"/> collection. </param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="P:NodeType"/> property equal to Lambda and the <see cref="P:Body"/> and <see cref="P:Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Expression body, string name, bool tailCall, IEnumerable<ParameterExpression> parameters) {
            ContractUtils.RequiresNotNull(body, "body");

            var parameterList = parameters.ToReadOnly();

            int paramCount = parameterList.Count;
            Type[] typeArgs = new Type[paramCount + 1];
            if (paramCount > 0) {
                var set = new Set<ParameterExpression>(parameterList.Count);
                for (int i = 0; i < paramCount; i++) {
                    var param = parameterList[i];
                    ContractUtils.RequiresNotNull(param, "parameter");
                    typeArgs[i] = param.IsByRef ? param.Type.MakeByRefType() : param.Type;
                    if (set.Contains(param)) {
                        throw Error.DuplicateVariable(param);
                    }
                    set.Add(param);
                }
            }
            typeArgs[paramCount] = body.Type;

            Type delegateType = DelegateHelpers.MakeDelegateType(typeArgs);

            return CreateLambda(delegateType, body, name, tailCall, parameterList);
        }

        /// <summary>
        /// Creates a LambdaExpression by first constructing a delegate type. 
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="P:Body"/> property equal to. </param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="P:Parameters"/> collection. </param>
        /// <param name="name">The name for the lambda. Used for emitting debug information.</param>
        /// <param name="delegateType">A <see cref="Type"/> representing the delegate signature for the lambda.</param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="P:NodeType"/> property equal to Lambda and the <see cref="P:Body"/> and <see cref="P:Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Type delegateType, Expression body, string name, IEnumerable<ParameterExpression> parameters) {
            var paramList = parameters.ToReadOnly();
            ValidateLambdaArgs(delegateType, ref body, paramList);

            return CreateLambda(delegateType, body, name, false, paramList);
        }

        /// <summary>
        /// Creates a LambdaExpression by first constructing a delegate type. 
        /// </summary>
        /// <param name="delegateType">A <see cref="Type"/> representing the delegate signature for the lambda.</param>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="P:Body"/> property equal to. </param>
        /// <param name="name">The name for the lambda. Used for emitting debug information.</param>
        /// <param name="tailCall">A <see cref="Boolean"/> that indicates if tail call optimization will be applied when compiling the created expression. </param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="P:Parameters"/> collection. </param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="P:NodeType"/> property equal to Lambda and the <see cref="P:Body"/> and <see cref="P:Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Type delegateType, Expression body, string name, bool tailCall, IEnumerable<ParameterExpression> parameters) {
            var paramList = parameters.ToReadOnly();
            ValidateLambdaArgs(delegateType, ref body, paramList);

            return CreateLambda(delegateType, body, name, tailCall, paramList);
        }

        private static void ValidateLambdaArgs(Type delegateType, ref Expression body, ReadOnlyCollection<ParameterExpression> parameters) {
            ContractUtils.RequiresNotNull(delegateType, "delegateType");
            RequiresCanRead(body, "body");

            if (!typeof(MulticastDelegate).IsAssignableFrom(delegateType) || delegateType == typeof(MulticastDelegate)) {
                throw Error.LambdaTypeMustBeDerivedFromSystemDelegate();
            }

            MethodInfo mi;
            var ldc = _LambdaDelegateCache;
            if (!ldc.TryGetValue(delegateType, out mi)) {
                mi = delegateType.GetMethod("Invoke");
                if (TypeUtils.CanCache(delegateType)) {
                    ldc[delegateType] = mi;
                }
            }

            ParameterInfo[] pis = mi.GetParametersCached();

            if (pis.Length > 0) {
                if (pis.Length != parameters.Count) {
                    throw Error.IncorrectNumberOfLambdaDeclarationParameters();
                }
                var set = new Set<ParameterExpression>(pis.Length);
                for (int i = 0, n = pis.Length; i < n; i++) {
                    ParameterExpression pex = parameters[i];
                    ParameterInfo pi = pis[i];
                    RequiresCanRead(pex, "parameters");
                    Type pType = pi.ParameterType;
                    if (pex.IsByRef) {
                        if (!pType.IsByRef) {
                            //We cannot pass a parameter of T& to a delegate that takes T or any non-ByRef type.
                            throw Error.ParameterExpressionNotValidAsDelegate(pex.Type.MakeByRefType(), pType);
                        }
                        pType = pType.GetElementType();
                    }
                    if (!TypeUtils.AreReferenceAssignable(pex.Type, pType)) {
                        throw Error.ParameterExpressionNotValidAsDelegate(pex.Type, pType);
                    }
                    if (set.Contains(pex)) {
                        throw Error.DuplicateVariable(pex);
                    }
                    set.Add(pex);
                }
            } else if (parameters.Count > 0) {
                throw Error.IncorrectNumberOfLambdaDeclarationParameters();
            }
            if (mi.ReturnType != typeof(void) && !TypeUtils.AreReferenceAssignable(mi.ReturnType, body.Type)) {
                if (!TryQuote(mi.ReturnType, ref body)) {
                    throw Error.ExpressionTypeDoesNotMatchReturn(body.Type, mi.ReturnType);
                }
            }
        }

        private static bool ValidateTryGetFuncActionArgs(Type[] typeArgs) {
            if (typeArgs == null) {
                throw new ArgumentNullException("typeArgs");
            }
            for (int i = 0, n = typeArgs.Length; i < n; i++) {
                var a = typeArgs[i];
                if (a == null) {
                    throw new ArgumentNullException("typeArgs");
                }
                if (a.IsByRef) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Creates a <see cref="Type"/> object that represents a generic System.Func delegate type that has specific type arguments.
        /// The last type argument specifies the return type of the created delegate.
        /// </summary>
        /// <param name="typeArgs">An array of Type objects that specify the type arguments for the System.Func delegate type.</param>
        /// <returns>The type of a System.Func delegate that has the specified type arguments.</returns>
        public static Type GetFuncType(params Type[] typeArgs) {
            if (!ValidateTryGetFuncActionArgs(typeArgs)) throw Error.TypeMustNotBeByRef();

            Type result = DelegateHelpers.GetFuncType(typeArgs);
            if (result == null) {
                throw Error.IncorrectNumberOfTypeArgsForFunc();
            }
            return result;
        }

        /// <summary>
        /// Creates a <see cref="Type"/> object that represents a generic System.Func delegate type that has specific type arguments.
        /// The last type argument specifies the return type of the created delegate.
        /// </summary>
        /// <param name="typeArgs">An array of Type objects that specify the type arguments for the System.Func delegate type.</param>
        /// <param name="funcType">When this method returns, contains the generic System.Func delegate type that has specific type arguments. Contains null if there is no generic System.Func delegate that matches the <paramref name="typeArgs"/>.This parameter is passed uninitialized.</param>
        /// <returns>true if generic System.Func delegate type was created for specific <paramref name="typeArgs"/>; false otherwise.</returns>
        public static bool TryGetFuncType(Type[] typeArgs, out Type funcType) {
            if (ValidateTryGetFuncActionArgs(typeArgs)) {
                return (funcType = DelegateHelpers.GetFuncType(typeArgs)) != null;
            }
            funcType = null;
            return false;
        }

        /// <summary>
        /// Creates a <see cref="Type"/> object that represents a generic System.Action delegate type that has specific type arguments. 
        /// </summary>
        /// <param name="typeArgs">An array of Type objects that specify the type arguments for the System.Action delegate type.</param>
        /// <returns>The type of a System.Action delegate that has the specified type arguments.</returns>
        public static Type GetActionType(params Type[] typeArgs) {
            if (!ValidateTryGetFuncActionArgs(typeArgs)) throw Error.TypeMustNotBeByRef();

            Type result = DelegateHelpers.GetActionType(typeArgs);
            if (result == null) {
                throw Error.IncorrectNumberOfTypeArgsForAction();
            }
            return result;
        }

        /// <summary>
        /// Creates a <see cref="Type"/> object that represents a generic System.Action delegate type that has specific type arguments.
        /// </summary>
        /// <param name="typeArgs">An array of Type objects that specify the type arguments for the System.Action delegate type.</param>
        /// <param name="actionType">When this method returns, contains the generic System.Action delegate type that has specific type arguments. Contains null if there is no generic System.Action delegate that matches the <paramref name="typeArgs"/>.This parameter is passed uninitialized.</param>
        /// <returns>true if generic System.Action delegate type was created for specific <paramref name="typeArgs"/>; false otherwise.</returns>
        public static bool TryGetActionType(Type[] typeArgs, out Type actionType) {
            if (ValidateTryGetFuncActionArgs(typeArgs)) {
                return (actionType = DelegateHelpers.GetActionType(typeArgs)) != null;
            }
            actionType = null;
            return false;
        }

        /// <summary>
        /// Gets a <see cref="Type"/> object that represents a generic System.Func or System.Action delegate type that has specific type arguments.
        /// The last type argument determines the return type of the delegate. If no Func or Action is large enough, it will generate a custom
        /// delegate type.
        /// </summary>
        /// <param name="typeArgs">The type arguments of the delegate.</param>
        /// <returns>The delegate type.</returns>
        /// <remarks>
        /// As with Func, the last argument is the return type. It can be set
        /// to System.Void to produce an Action.</remarks>
        public static Type GetDelegateType(params Type[] typeArgs) {
            ContractUtils.RequiresNotEmpty(typeArgs, "typeArgs");
            ContractUtils.RequiresNotNullItems(typeArgs, "typeArgs");
            return DelegateHelpers.MakeDelegateType(typeArgs);
        }
    }
}
