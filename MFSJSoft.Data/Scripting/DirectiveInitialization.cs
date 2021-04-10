
using System;

namespace MFSJSoft.Data.Scripting
{

    [Flags]
    public enum DirectiveInitializationAction
    {
        DEFAULT = 0,
        NO_STORE = 1,
        REPLACE_TEXT = 2,
        DEFER_SETUP = 4
    }

    public class DirectiveInitialization
    {

        public DirectiveInitialization(object initializedState = default, DirectiveInitializationAction action = default, string replacementText = default)
        {
            Action = action;
            ReplacementText = replacementText;
            InitializedState = initializedState;
        }


        public DirectiveInitializationAction Action { get; set; }
        public string ReplacementText { get; set; }
        public object InitializedState { get; set; }

    }
}
