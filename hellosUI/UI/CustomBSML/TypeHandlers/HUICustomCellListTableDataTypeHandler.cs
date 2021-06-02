using System.Collections.Generic;
using UnityEngine;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.TypeHandlers;
using HUI.UI.CustomBSML.Components;

namespace HUI.UI.CustomBSML.TypeHandlers
{
    [ComponentHandler(typeof(HUICustomCellListTableData))]
    public class HUICustomCellListTableDataTypeHandler : CustomCellListTableDataHandler
    {
        public override Dictionary<string, string[]> Props
        {
            get
            {
                var props = base.Props;

                props.Add("hoveredColor", new[] { "hovered-color" });
                props.Add("selectedColor", new[] { "selected-color" });

                return props;
            }
        }

        public override void HandleType(BSMLParser.ComponentTypeWithData componentType, BSMLParserParams parserParams)
        {
            base.HandleType(componentType, parserParams);

            HUICustomCellListTableData tableData = componentType.component as HUICustomCellListTableData;
            if (componentType.data.TryGetValue("hoveredColor", out string colourString))
            {
                if (ColorUtility.TryParseHtmlString(colourString, out Color colour))
                    tableData.HoveredColour = colour;
                else
                    Plugin.Log.Warn($"Colour {colourString} is not a valid colour");
            }
            if (componentType.data.TryGetValue("selectedColor", out colourString))
            {
                if (ColorUtility.TryParseHtmlString(colourString, out Color colour))
                    tableData.SelectedColour = colour;
                else
                    Plugin.Log.Warn($"Colour {colourString} is not a valid colour");
            }
        }
    }
}
