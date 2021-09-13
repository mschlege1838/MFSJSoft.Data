using MFSJSoft.Data.Scripting.Model;
using System;

namespace MFSJSoft.Data.Scripting.Processor
{

    /// <summary>
    /// Internal base class for <see cref="IProperties"/>-backed <see cref="IDirectiveProcessor"/> implementations
    /// that update statement text with property values. Application code will generally not need to use this.
    /// </summary>
    /// <remarks>
    /// The <see cref="DirectiveInitializationAction.ReplaceText"/> action is used for text replacement. If the
    /// <c>deferRuntime</c> argument to the constructor is <see langword="true"/>,
    /// <see cref="DirectiveInitializationAction.DeferSetup"/> will be specified, otherwise
    /// <see cref="DirectiveInitializationAction.NoStore"/>.
    /// </remarks>
    public class PropertiesReplaceProcessor : PropertiesEvaluator, IDirectiveProcessor
    {

        readonly string directiveName;
        readonly string negatedDirectiveName;
        readonly bool deferRuntime;

        /// <summary>
        /// Primary constructor.
        /// </summary>
        /// <param name="directiveName">Name of the directive the derived class handles.</param>
        /// <param name="negatedDirectiveName">Name of directive the derived class handles when negation is to be applied.</param>
        /// <param name="properties">Backing <see cref="IProperties"/>. Uses <see cref="PropertiesDirectiveConfiguration.Properties"/>
        /// from global configuration if <see langword="null" /></param>
        /// <param name="deferRuntime"><see langword="true"/> to <see cref="DirectiveInitializationAction.DeferSetup">defer</see> to
        /// execution time, otherwise <see langword="false"/>.</param>
        public PropertiesReplaceProcessor(string directiveName, string negatedDirectiveName, IProperties properties, bool deferRuntime) : base(properties)
        {
            this.directiveName = string.IsNullOrEmpty(directiveName) ? throw new ArgumentNullException(nameof(directiveName)) : directiveName;
            this.negatedDirectiveName = string.IsNullOrEmpty(negatedDirectiveName) ? throw new ArgumentNullException(nameof(negatedDirectiveName)) : negatedDirectiveName;
            this.deferRuntime = deferRuntime;
        }

        /// <summary>
        /// Calls <see cref="PropertiesEvaluator.Init(object)"/>.
        /// </summary>
        /// <param name="context">Not used.</param>
        /// <param name="configuration">Global configuration.</param>
        public void InitProcessor(CompositeProcessorContext context, object configuration)
        {
            Init(configuration);
        }

        /// <summary>
        /// Evaluates the given <see cref="ScriptDirective"/>.
        /// </summary>
        /// <remarks>
        /// <para>If the given <c>directive</c> matches the <c>directiveName</c> or <c>negatedDirectiveName</c> given in the constructor,
        /// the directive will be evaluated.</para>
        /// 
        /// <para>Properties replace directives have between 2 and 4 arguments:</para>
        /// <list type="number">
        ///     <item>
        ///         <term><c>propertyName</c></term>
        ///         <description>Property name.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>replaceValue</c> OR <c>propertyValue</c></term>
        ///         <description>If argument count is two, value to replace in statement text if <c>propertyName</c> is defined.
        ///         Otherwise, the value to evaluate <c>propertyName</c> against to determine replacement.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>replaceValue</c></term>
        ///         <description>Value to replace in statement text if <c>propertyName</c> equals <c>propertyValue</c>, following
        ///         the semantics described in <see cref="PropertiesEvaluator.Eval"/>.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>elseValue</c></term>
        ///         <description>Value to replace in statement text if the evaluation fails. Default is the empty string.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <param name="context">Not used.</param>
        /// <param name="directive"><see cref="ScriptDirective"/> to process.</param>
        /// <returns>Appropriate <see cref="DirectiveInitialization"/> if <c>directive</c> is supported, otherwise <see langword="null" />.</returns>
        public DirectiveInitialization InitDirective(CompositeProcessorContext context, ScriptDirective directive)
        {
            bool negated;
            if (directive.Name == directiveName)
            {
                negated = false;
            }
            else if (directive.Name == negatedDirectiveName)
            {
                negated = true;
            }
            else
            {
                return null;
            }

            if (directive.Arguments.Count < 2)
            {
                throw new InvalidDirectiveException($"{directiveName} must have at least 2 arguments (property name, replace value).", directive);
            }
            if (directive.Arguments.Count > 4)
            {
                throw new InvalidDirectiveException($"{directiveName} can have at most 4 arguments.", directive);
            }

            var propertyName = directive.Arguments[0];
            var propertyValue = directive.Arguments.Count > 2 ? directive.Arguments[1] : null;
            var replaceValue = directive.Arguments[directive.Arguments.Count > 2 ? 2 : 1];
            var elseValue = directive.Arguments.Count > 3 ? directive.Arguments[3] : null;

            if (deferRuntime)
            {
                return new DirectiveInitialization(new InitializedState(propertyName, replaceValue, propertyValue, elseValue, negated), DirectiveInitializationAction.DeferSetup);
            }

            if (Eval(propertyName, propertyValue, negated))
            {
                return new DirectiveInitialization(action: DirectiveInitializationAction.ReplaceText | DirectiveInitializationAction.NoStore, replacementText: replaceValue);
            }
            else
            {
                if (elseValue is not null)
                {
                    return new DirectiveInitialization(action: DirectiveInitializationAction.ReplaceText | DirectiveInitializationAction.NoStore, replacementText: elseValue);
                }
                else
                {
                    return new DirectiveInitialization(action: DirectiveInitializationAction.NoStore);
                }
            }
        }

        /// <summary>
        /// Performs property <see cref="PropertiesEvaluator.Eval">evaluation</see> if <c>deferRuntime</c> was specified in the constructor.
        /// </summary>
        /// <param name="context">Not used.</param>
        /// <param name="directive"><see cref="ScriptDirective"/> to process.</param>
        /// <param name="initState">Processed directive argument information from <see cref="InitDirective"/>.</param>
        /// <returns></returns>
        public DirectiveInitialization SetupDirective(CompositeProcessorContext context, ScriptDirective directive, object initState)
        {
            if (!deferRuntime)
            {
                throw new NotImplementedException();
            }

            var initData = (InitializedState) initState;
            if (Eval(initData.PropertyName, initData.PropertyValue, initData.Negated))
            {
                return new DirectiveInitialization(action: DirectiveInitializationAction.ReplaceText, replacementText: initData.ReplaceValue);
            }
            else
            {
                if (initData.ElseValue is not null)
                {
                    return new DirectiveInitialization(action: DirectiveInitializationAction.ReplaceText, replacementText: initData.ElseValue);
                }
                else
                {
                    return new DirectiveInitialization();
                }
            }
        }

        /// <summary>
        /// Always returns <see langword="false"/> (does nothing). All necessary operations are performed in <see cref="InitDirective"/>
        /// and <see cref="SetupDirective" />, if applicable.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="text"></param>
        /// <param name="directive"></param>
        /// <param name="initState"></param>
        /// <returns><see langword="false"/></returns>
        public bool TryExecute(CompositeProcessorContext context, string text, ScriptDirective directive, object initState)
        {
            return false;
        }

        class InitializedState
        {
            internal InitializedState(string propertyName, string replaceValue, string propertyValue, string elseValue, bool negated)
            {
                PropertyName = propertyName;
                ReplaceValue = replaceValue;
                PropertyValue = propertyValue;
                ElseValue = elseValue;
                Negated = negated;
            }

            internal string PropertyName { get; }
            internal string ReplaceValue { get; }
            internal string PropertyValue { get; }
            internal string ElseValue { get; }
            internal bool Negated { get; }
        }
    }
}
