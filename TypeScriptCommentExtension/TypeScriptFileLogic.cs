using System;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace TypeScriptCommentExtension
{
    public class TypeScriptFileLogic
    {
        private readonly IWpfTextView view;
        private readonly ITextBuffer editor;
        private string tabs = "";
        private const string ERROR_MSG_PREFIX = "TypeScriptCommentExtension has encountered an error:\n";

        public TypeScriptFileLogic(IWpfTextView view)
        {
            this.view = view;
            this.editor = this.view.TextBuffer;
            this.editor.Changed += OnTextChanged;
        }

        /// <summary>
        /// On text change, check for the /**
        /// </summary>
        private void OnTextChanged(object sender, TextContentChangedEventArgs e)
        {
            try
            {
                if (!StubUtils.Options.TypeScriptCommentExtensionEnabled) 
                { 
                    return; 
                }

                INormalizedTextChangeCollection changes = e.Changes;

                foreach (ITextChange change in changes)
                {
                    if (CurrentTypedCharacterIs(change, "*") && PeviewTextTypedIs(change, "/*"))
                    {
                        CreateMethodComment(change.NewEnd, change);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ERROR_MSG_PREFIX + ex.Message);
            }
        }

        private static bool CurrentTypedCharacterIs(ITextChange change, string character)
        {
            return change.NewText.EndsWith(character);
        }

        private bool PeviewTextTypedIs(ITextChange change, string character)
        {
            int i = change.OldEnd;
            string lineText = this.view.TextSnapshot.GetLineFromPosition(i - 1).GetText();
            return lineText.EndsWith(character);
        }

        /// <summary>
        /// Creates a new comment line with appropriate spacing.
        /// </summary>
        /// <returns></returns>
        private string NewLine()
        {
            var result = Environment.NewLine + this.tabs + " * ";
            return result;
        }

        private void CreateMethodComment(int position, ITextChange change)
        {
            string text = this.view.TextSnapshot.ToString();
            using (ITextEdit editor = this.view.TextBuffer.CreateEdit())
            {
                try
                {
                    this.tabs = StubUtils.GetIndention(position, this.view.TextSnapshot);
                    string summaryString = StubUtils.Options.MultiLineSummary ? NewLine() : "";
                    string parameters = GetFunctionParameters(position);
                    string returnTag = GetReturnTag(position);
                    string commentBody = summaryString + parameters + returnTag;
                    string autoComment = this.tabs + "/**" + commentBody;
                    if (!String.IsNullOrEmpty(commentBody))
                    {
                        autoComment += Environment.NewLine + this.tabs;
                    }

                    autoComment += " */";

                    int lineStart = this.view.TextSnapshot.GetLineFromPosition(position).Start.Position;
                    Span firstLineSpan = new Span(lineStart, change.NewSpan.End - lineStart);
                    editor.Replace(firstLineSpan, autoComment);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ERROR_MSG_PREFIX + ex.Message);
                }
            }
        }

        private string GetFunctionParameters(int position)
        {
            var parameters = StubUtils.GetFunctionParameters(position, this.view.TextSnapshot);
            var result = "";

            foreach (string param in parameters)
            {
                string name = StubUtils.GetParamName(param);
                string type = StubUtils.GetParamType(param);
                if (!String.IsNullOrEmpty(name))
                    result += NewLine() + CreateParamString(name, type);
            }

            return result;
        }

        private string CreateParamString(string name, string type)
        {
            var result = "@param ";
            if (!String.IsNullOrEmpty(type))
            {
                result += "{" + type + "} ";
            }

            return result + name;
        }

        /// <summary>
        /// Returns a string for a return tag if one is necessary.
        /// </summary>
        /// <param name="position">Position of the last slash in the triple slash comment</param>
        /// <returns>Return tag line as a string.</returns>
        private string GetReturnTag(int position)
        {
            string shouldCreate = StubUtils.ShouldCreateReturnTag(position, this.view.TextSnapshot);

            string result = NewLine() + "@returns {" + shouldCreate + "}";

            return result;
        }

    }
}
