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

        //Microsoft.VisualStudio.Text.Impl
        /// <summary>
        /// On text change, check for the /// comment.
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
                    if (change.NewText.EndsWith("*") && LineIsJSDocOpening(change.OldEnd))
                    {
                        CreateStub(change.NewEnd, change);
                    }
                    else if (StubUtils.Options.AutoNewLine && change.NewText.EndsWith(Environment.NewLine))
                    {
                        CreateNewCommentLine(change);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ERROR_MSG_PREFIX + ex.Message);
            }
        }

        /// <summary>
        /// Returns true if the line at the given position ends with the /* characters prior to any pending changes.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private bool LineIsJSDocOpening(int i)
        {
            return StubUtils.GetLineTextFromPosition(i, this.view.TextSnapshot).EndsWith("/*");
        }

        private static readonly Regex commentLineStart = new Regex(@"^\s*(\*)|(/\*\*)", RegexOptions.Compiled);
        private void CreateNewCommentLine(ITextChange change)
        {
            using (ITextEdit editor = this.view.TextBuffer.CreateEdit())
            {
                try
                {
                    ITextSnapshotLine line = this.view.TextSnapshot.GetLineFromPosition(change.OldEnd);
                    string lineText = line.GetText();
                    string nextLine = this.view.TextSnapshot.GetLineFromLineNumber(line.LineNumber + 1).GetText();
                    if (commentLineStart.IsMatch(lineText) && (commentLineStart.IsMatch(nextLine) || change.OldEnd != line.End.Position))
                    {
                        int asteriskIndex = lineText.IndexOf('*');
                        //Only add a new comment line if the newline char is after the triple slash
                        //(how Visual Studio in C# works)
                        if ((line.Start.Position + asteriskIndex) > change.OldEnd)
                            return;

                        int openingSlashIndex = lineText.IndexOf('/');
                        int tabsStopIndex = asteriskIndex > openingSlashIndex ? openingSlashIndex : asteriskIndex;

                        string newTabs = tabsStopIndex >= 0 ? lineText.Substring(0, tabsStopIndex) : "";
                        editor.Replace(change.NewSpan, Environment.NewLine + newTabs + "* ");
                        editor.Apply();
                    }
                }
                catch (Exception) { }
            }
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

        private void CreateStub(int position, ITextChange change)
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
            string result = "";
            bool shouldCreate = StubUtils.ShouldCreateReturnTag(position, this.view.TextSnapshot);

            if (shouldCreate)
                result = NewLine() + "@returns";

            return result;
        }

    }
}
