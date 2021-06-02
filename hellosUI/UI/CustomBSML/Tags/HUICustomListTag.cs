using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Tags;
using UnityEngine;
using HUI.UI.CustomBSML.Components;

namespace HUI.UI.CustomBSML.Tags
{
    public class HUICustomListTag : CustomListTag
    {
        public override string[] Aliases => new[] { "hui-custom-list" };

        public override GameObject CreateObject(Transform parent)
        {
            var containerGO = base.CreateObject(parent);

            Object.Destroy(containerGO.GetComponent<CustomCellListTableData>());

            var tableData = containerGO.AddComponent<HUICustomCellListTableData>();
            var tableView = containerGO.transform.Find("BSMLCustomList").GetComponent<BSMLTableView>();

            tableData.tableView = tableView;
            tableView.SetDataSource(tableData, false);

            return containerGO;
        }
    }
}
