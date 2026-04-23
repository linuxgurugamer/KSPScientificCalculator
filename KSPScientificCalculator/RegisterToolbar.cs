using ClickThroughFix;
using Expansions.Missions.Editor;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolbarControl_NS;
using UnityEngine;
using UnityEngine.UIElements;

namespace KSPScientificCalculator
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        public void Start()
        {
            ToolbarControl.RegisterMod(CalculatorCore.MODID, CalculatorCore.MODNAME);
        }

        bool initted = false;

        public void OnGUI()
        {
            if (initted)
                return;
            InitializeStyles();
            initted = true;
        }
        private void InitializeStyles()
        {
            CalculatorCore.displayStyle = new GUIStyle(HighLogic.Skin.label) { alignment = TextAnchor.MiddleRight, fontSize = 16, wordWrap = true };
            CalculatorCore.resultStyle = new GUIStyle(HighLogic.Skin.label) { alignment = TextAnchor.MiddleRight, fontSize = 20, fontStyle = FontStyle.Bold };
            CalculatorCore.historyStyle = new GUIStyle(HighLogic.Skin.button) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
            CalculatorCore.statusStyle = new GUIStyle(HighLogic.Skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = 12, wordWrap = true };
        }


    }

}
