using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace TypeScriptCommentExtension
{
    #region Adornment Factory
    /// <summary>
    /// The extension will be listening specific file type. We specify that we want only TypeScript file.
    /// This class also setup the extension to be executed at a particular time which is after any selection and before text.
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("TypeScript")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class CommentAdornmentFactory : IWpfTextViewCreationListener
    {
        /// <summary>
        /// Defines the adornment layer for the adornment. This layer is ordered 
        /// after the selection layer in the Z-order
        /// </summary>
        [Export(typeof(AdornmentLayerDefinition))]
        [Name("TypeScriptCommentExtension")]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
        public AdornmentLayerDefinition editorAdornmentLayer = null;
                
        /// <summary>
        /// Instantiates a extension manager when a textView is created.
        /// </summary>
        /// <param name="textView">The <see cref="IWpfTextView"/> upon which the adornment should be placed</param>
        public void TextViewCreated(IWpfTextView textView)
        {
            new TypeScriptFileLogic(textView);
        }
    }
    #endregion //Adornment Factory

}
