
using System;

namespace MFSJSoft.Data.Scripting
{

    [Flags]
    public enum DirectiveInitializationAction
    {
        Default = 0,
        NoStore = 1,
        ReplaceText = 2,
        DeferSetup = 4
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
