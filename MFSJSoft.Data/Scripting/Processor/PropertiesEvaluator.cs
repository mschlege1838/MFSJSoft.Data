using System;

namespace MFSJSoft.Data.Scripting.Processor
{
    public abstract class PropertiesEvaluator
    {

        protected readonly IProperties properties;

        public PropertiesEvaluator(IProperties properties)
        {
            this.properties = properties ?? throw new ArgumentNullException(nameof(properties));
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
