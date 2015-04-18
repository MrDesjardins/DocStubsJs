using System;
using System.Globalization;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using System.Windows.Forms.ComponentModel.Com2Interop;
using TypeScriptCommentOptions;

namespace JScriptStubOptions
{
    public enum ReturnTagGenerationSetting
    {
        Auto, Always, Never
    }
    [Guid(GuidList.guidJScriptStubOptionsCmdSetString)]
    [ComVisible(true)]
    class Options : DialogPage
    {

        #region Properties
        private bool autoNewLine = true;
        /// <summary>
        /// Gets or sets whether or not the new comment lines should be auto-generated.
        /// </summary>
        [Category("General")]
        [DisplayName("Auto New Line")]
        [Description("Whether or not to auto-create a new comment line when inside documentation comments.")]
        public bool AutoNewLine
        {
            get
            {
                return autoNewLine;
            }
            set
            {
                autoNewLine = value;
            }
        }

        private bool multiLineSummary = true;
        /// <summary>
        /// Gets or sets whether or not the summary tags should be generated on multiple lines.
        /// </summary>
        [Category("General")]
        [DisplayName("Multi-line Summary Tags")]
        [Description("Whether or not a blank line should be added for the summary/description.")]
        public bool MultiLineSummary
        {
            get
            {
                return multiLineSummary;
            }
            set
            {
                multiLineSummary = value;
            }
        }

        private ReturnTagGenerationSetting returnGenerationSetting = ReturnTagGenerationSetting.Auto;
        /// <summary>
        /// Gets or sets how return tags should be generated.
        /// </summary>
        [Category("General")]
        [DisplayName("Generate Return Tag")]
        [Description("Determines when return tags should be created.")]
        public ReturnTagGenerationSetting ReturnGenerationSetting
        {
            get
            {
                return returnGenerationSetting;
            }
            set
            {
                returnGenerationSetting = value;
            }
        }

      

        private bool jsdocEnabled = true;
        [Category("JSDoc Options")]
        [DisplayName("Enabled")]
        public bool JSDocEnabled
        {
            get
            {
                return jsdocEnabled;
            }
            set
            {
                jsdocEnabled = value;
            }
        }

        private bool useAsterisk = true;
        [Category("JSDoc Options")]
        [DisplayName("Include Asterisk (*)")]
        [Description("Whether or not JSDoc comment lines should all start with an asterisk (*).")]
        public bool UseAsterisk
        {
            get
            {
                return useAsterisk;
            }
            set
            {
                useAsterisk = value;
            }
        }
        #endregion Properties

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);
            SaveSettingsToStorage();
            ReturnOptions.OptionsChanged = true;
        }

        public override void ResetSettings()
        {
            base.ResetSettings();
            this.MultiLineSummary = false;
            this.UseAsterisk = true;
            this.JSDocEnabled = true;
            ReturnOptions.OptionsChanged = true;
        }
    }
}
