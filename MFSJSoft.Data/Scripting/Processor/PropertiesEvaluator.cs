using System;

namespace MFSJSoft.Data.Scripting.Processor
{

    public class PropertiesDirectiveConfiguration
    {
        public IProperties Properties { get; set; }
    }

    public abstract class PropertiesEvaluator
    {

        IProperties properties;

        public PropertiesEvaluator(IProperties properties = null)
        {
            this.properties = properties;
        }

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

        protected bool Eval(string propName, string propValue, bool negate)
        {
            var value = properties.GetProperty(propName);
            if (value is null)
            {
                return false;
            }

            var result = propValue is null || propValue == "*" ? value.ToLower() != "false" : value == propValue;
            return negate ? !result : result;
        }
    }

}
