using System;
using System.Linq;
using System.Text.RegularExpressions;
using JScriptStubOptions;
using Microsoft.VisualStudio.Text;
using TypeScriptCommentOptions;

namespace TypeScriptCommentExtension
{
    class StubUtils
    {
        public static readonly Regex ReturnRegex = new Regex("return ");
        public static readonly Regex JavaScriptFnRegex = new Regex(@"function(\(|\s)");

        // Currently, this regex could give some false-positives. Ex: { someProp: (a * b) };
        public static readonly Regex TypeScriptFnRegex = new Regex(@":\s?\([a-z_$]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly ReturnOptions Options = new ReturnOptions();

        public static string GetLineTextFromPosition(int position, ITextSnapshot snapshot)
        {
            return snapshot.GetLineFromPosition(position - 1).GetText();
        }

        /// <summary>
        /// Gets the number of tabs from the beginning of the line.
        /// </summary>
        /// <param name="lastSlashPosition"></param>
        /// <param name="capture">The snapshot to use as the context of the line.</param>
        /// <returns></returns>
        public static string GetIndention(int lastSlashPosition, ITextSnapshot capture)
        {
            int lineNum = capture.GetLineNumberFromPosition(lastSlashPosition);
            lineNum++; 
            lineNum = GetFunctionDeclarationLineNumber(capture, lineNum);
            string space = capture.GetLineFromLineNumber(lineNum).GetText();
            int leadingSpace = space.Length - space.TrimStart().Length;
            space = space.Substring(0, leadingSpace);

            return space; 
        }


        private static bool IsFunctionLine(string lineText, bool isTypeScript)
        {
            return JavaScriptFnRegex.IsMatch(lineText) || (isTypeScript && TypeScriptFnRegex.IsMatch(lineText));
        }

        /// <summary>
        /// Returns the line on which the word "function" or other function initializers appear.
        /// </summary>
        /// <param name="capture">The text snapshot.</param>
        /// <param name="lineNumber">The line that should contain the open curlybrace for the function if one exists
        /// in the context of the comment, or the first line of the function itself.</param>
        /// <returns>Returns the line of the function declaration. -1 if one is not found that corresponds to the given
        /// line number.</returns>
        public static int GetFunctionDeclarationLineNumber(ITextSnapshot capture, int lineNumber)
        {
            string lineText = capture.GetLineFromLineNumber(lineNumber).GetText();
            var isTypeScript = capture.ContentType.TypeName == "TypeScript";
            if (!IsFunctionLine(lineText, isTypeScript)) 
            { 
                return -1; 
            }

            return lineNumber;
        }

        /// <summary>
        /// Returns the given string with text that falls in a comment block removed.
        /// </summary>
        /// <param name="text">The string that may or maynot contain comments.</param>
        /// <returns></returns>
        public static string RemoveComments(string text)
        {
            if (text.Contains("//"))
            {
                return text.Substring(0, text.IndexOf("//"));
            }
            else if (text.Contains("/*"))
            {
                if (!text.Contains("*/"))
                    return text.Substring(0, text.IndexOf("/*"));
                else
                {
                    string result = text.Substring(0, text.IndexOf("/*"));
                    //Add 2 to only include characters after the */ string.
                    result += text.Substring(text.IndexOf("*/") + 2);
                    return RemoveComments(result);
                }
            }
            else
                return text;
        }

        public static string[] GetFunctionParameters(int position, ITextSnapshot capture)
        {

            int openFunctionLine = capture.GetLineNumberFromPosition(position - 1);
            openFunctionLine += 1;
            ITextSnapshotLine line = capture.GetLineFromLineNumber(openFunctionLine);
            string prevLine = line.Extent.GetText();
            openFunctionLine = StubUtils.GetFunctionDeclarationLineNumber(capture, openFunctionLine);
            //Not immediately after a function declaration
            if (openFunctionLine == -1) return new string[0];

            prevLine = capture.GetLineFromLineNumber(openFunctionLine).GetText();

            int ftnIndex = StubUtils.JavaScriptFnRegex.Match(prevLine).Index;
            int firstParenPosition = -1;
            if (prevLine.IndexOf('(', ftnIndex) > -1)
            {
                firstParenPosition = capture.GetLineFromLineNumber(openFunctionLine).Start +
                                 prevLine.IndexOf('(', ftnIndex) + 1;
            }
            else
            {
                do
                {
                    openFunctionLine++;
                    prevLine = capture.GetLineFromLineNumber(openFunctionLine).GetText();
                } while (!prevLine.Contains("("));

                firstParenPosition = capture.GetLineFromLineNumber(openFunctionLine).Start
                                     + prevLine.IndexOf('(')
                                     + 1;
            }

            int lastParenPosition = -1;
            if (prevLine.IndexOf(')') > 0)
            {
                lastParenPosition = capture.GetLineFromLineNumber(openFunctionLine).Start + prevLine.IndexOf(')', prevLine.IndexOf('('));
            }
            else
            {
                do
                {
                    openFunctionLine++;
                    prevLine = capture.GetLineFromLineNumber(openFunctionLine).GetText();
                } while (!prevLine.Contains(")"));

                lastParenPosition = capture.GetLineFromLineNumber(openFunctionLine).Start +prevLine.IndexOf(")");
            }


            return StubUtils
                .RemoveComments(capture
                    .GetText()
                    .Substring(firstParenPosition, (lastParenPosition - firstParenPosition)))
                .Split(',')
                .Select(param => param.Trim())
                .ToArray();
        }

        public static bool ShouldCreateReturnTag(int position, ITextSnapshot capture)
        {

            if (Options.ReturnGenerationOption == ReturnTagGenerationSetting.Always)
            {
                return true;
            }
            if (Options.ReturnGenerationOption == ReturnTagGenerationSetting.Never)
            {
                return false;
            }

            bool hasReturn = false;
            bool newFunction = false;
            bool functionClosed = false;
            bool hasComment = false;
            int lineNumber = capture.GetLineNumberFromPosition(position - 1);
            string lineText = capture.GetLineFromLineNumber(lineNumber).GetText();

            lineNumber++;
   

            bool inFunction = GetFunctionDeclarationLineNumber(capture, lineNumber) >= 0;
            if (!inFunction) return false;

            lineNumber = GetNextOpenCurlyBrace(lineNumber, capture);

            if (lineNumber == -1) { return false; }

            int functionsOpen = 1;
            int openBracket = 1;

            for (int i = lineNumber; i < capture.LineCount; i++)
            {
                lineText = capture.GetLineFromLineNumber(i).GetText();
                //HANDLE COMMENTS
                if (lineText.Contains("/*") && lineText.Contains("*/") && lineText.LastIndexOf("/*") > lineText.LastIndexOf("*/"))
                {
                    hasComment = true;
                }
                else if (lineText.Contains("/*") && lineText.Contains("*/"))
                {
                    hasComment = false;
                }
                else if (lineText.Contains("/*"))
                {
                    hasComment = true;
                }

                if (hasComment && lineText.Contains("*/"))
                {
                    if (!lineText.Contains("/*") || lineText.LastIndexOf("/*") <= lineText.LastIndexOf("*/"))
                        hasComment = false;
                }
                else if (hasComment || String.IsNullOrEmpty(lineText.Trim())) { continue; }

                lineText = RemoveComments(lineText);

                //END COMMENT HANDLING

                //HANDLE BRACKETS - "{ }"
                if (JavaScriptFnRegex.IsMatch(lineText) && lineText.Contains("{"))
                {
                    //adds an open function and an open bracket.
                    functionsOpen++;
                }
                else if (JavaScriptFnRegex.IsMatch(lineText))
                {
                    //states that there is a new function open without an open bracket.
                    newFunction = true;
                }
                else if (newFunction && lineText.Contains("{"))
                {
                    //states that there is no longer a new function and adds an open
                    //bracket and open function.
                    newFunction = false;
                    functionsOpen++;
                }

                if (lineText.Contains("{"))
                {
                    //Adds an open bracket.
                    openBracket++;
                }
                bool isInlineFunction = false;

                if (lineText.Contains("}"))
                {
                    //If function is closed on same line as closing bracket
                    if (functionsOpen == 1 && ReturnRegex.IsMatch(lineText))
                    {
                        hasReturn = true;
                        break;
                    }
                    else if (ReturnRegex.IsMatch(lineText))
                        isInlineFunction = true;
                    //Decrements both the number of open brackets and functions if they are equal.
                    //This means the number of open brackets are the same as the number of open functions.
                    //Otherwise it just decrements the number of open brackets.
                    if (openBracket == functionsOpen)
                    {
                        functionsOpen--;
                    }

                    openBracket--;
                }
                if (functionsOpen == 0) functionClosed = true;

                if (functionsOpen == 1 && ReturnRegex.IsMatch(lineText) && !isInlineFunction)
                {
                    hasReturn = true;
                    break;
                }
                else if (functionClosed)
                {
                    break;
                }
            }
            return hasReturn;
        }

        // Searches down the file for the next line with an open curly brace, including the given line number.
        // Returns the line number.
        private static int GetNextOpenCurlyBrace(int lineNumber, ITextSnapshot capture)
        {
            var found = false;
            while (lineNumber < capture.LineCount)
            {
                var text = capture.GetLineFromLineNumber(lineNumber).GetText();
                if (text.Contains("{"))
                {
                    found = true;
                    break;
                }

                lineNumber++;
            }

            if (found == false) { return -1; }

            return lineNumber;
        }

        /// <summary>
        /// Returns the param name from the given parameter definition regardless of whether it is for JavaScript or TypeScript.
        /// </summary>
        /// <param name="paramDefinition"></param>
        /// <returns></returns>
        public static string GetParamName(string paramDefinition)
        {
            if (!paramDefinition.Contains(':')) { return paramDefinition; }

            return paramDefinition.Split(':')[0].Trim();
        }

        /// <summary>
        /// Returns the param type from the given parameter definition. If it is not found, returns null.
        /// </summary>
        /// <param name="paramDefinition"></param>
        /// <returns></returns>
        public static string GetParamType(string paramDefinition)
        {
            if (!paramDefinition.Contains(':')) { return null; }

            return paramDefinition.Split(':')[1].Trim();
        }
    }
}
