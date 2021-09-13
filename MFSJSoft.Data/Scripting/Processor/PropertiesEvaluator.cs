using System;

namespace MFSJSoft.Data.Scripting.Processor
{

    /// <summary>
    /// Global configuration object for <see cref="PropertiesEvaluator"/> derivitives.
    /// </summary>
    public class PropertiesDirectiveConfiguration
    {

        /// <summary>
        /// Backing <see cref="IProperties"/> to use when evaluating properties.
        /// </summary>
        public IProperties Properties { get; set; }
    }

    /// <summary>
    /// Internal base class for evaluating properties within the context of script execution. Generally, application code will
    /// not need to use this.
    /// </summary>
    /// <remarks>
    /// <see cref="IProperties"/> can be provided as a constructor argument, or keyed under a derived class' runtime
    /// <see cref="object.GetType">type</see> as <see cref="PropertiesDirectiveConfiguration.Properties"/> in global configuration.
    /// </remarks>
    public abstract class PropertiesEvaluator
    {

        IProperties properties;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="properties">Optional <see cref="IProperties"/>. If not given here, must be in global configuration.</param>
        public PropertiesEvaluator(IProperties properties = null)
        {
            this.properties = properties;
        }

        /// <summary>
        /// Sets the backing <see cref="IProperties"/> if not given in the constructor, and a non-<see langword="null" /> 
        /// <see cref="PropertiesDirectiveConfiguration.Properties"/> instance is found keyed under the derived class' 
        /// runtime <see cref="object.GetType">type</see>.
        /// </summary>
        /// <param name="configuration">Global configuration.</param>
        protected void Init(object configuration)
        {
            if (configuration is not null && configuration is PropertiesDirectiveConfiguration lcfg)
            {
                if (properties is null && lcfg.Properties is not null)
                {
                    properties = lcfg.Properties;
                }
            }

            if (properties is null)
            {
                throw new NullReferenceException($"Properties must be either passed to the constructor, or given as global configuration for properties directives: {GetType()}");
            }
        }

        /// <summary>
        /// Evaluates the given property name.
        /// </summary>
        /// <remarks>
        /// If <c>propValue</c> is <see langword="null" />, or equal to the character string <c>"*"</c>, will return <see langword="true"/>
        /// if the resolved property value is not equal to (ignoring case) <c>"false"</c>. If <c>propValue</c> is given, will return
        /// <see langword="true" /> if the resolved property value is equal to (considering case) <c>propValue</c>. If <c>negate</c>
        /// is <see langword="true"/>, the return value will be negated.
        /// </remarks>
        /// <param name="propName">Property name to resolve.</param>
        /// <param name="propValue">Property value to test against. Pass <see langword="null" /> or <c>"*"</c> to only test whether
        /// the property is defined.</param>
        /// <param name="negate"><see langword="true"/> to negate the result.</param>
        /// <returns><see langword="true"/> if the given <c>propName</c> is defined, and equal to <c>propValue</c>, if applicable. If
        /// <c>propValue</c> is not given or <c>"*"</c>, will return <see langword="true"/> if <c>propName</c> is defined.</returns>
        protected bool Eval(string propName, string propValue, bool negate)
        {
            var value = properties.GetProperty(propName);
            if (value is null)
            {
                return negate;
            }

            var result = propValue is null || propValue == "*" ? value.ToLower() != "false" : value == propValue;
            return negate ? !result : result;
        }
    }

}
