using System;
using System.Collections.Generic;
using TriInspector;
using UnityEngine;

public class Collections_TableListSample : ScriptableObject
{
    #region カスタマイズ: ListDrawerSettings側でのテーブル対応

    // [TableList(Draggable = true,
    //     HideAddButton = false,
    //     HideRemoveButton = false,
    //     AlwaysExpanded = false)]
    [ListDrawerSettings(Draggable = true,
        HideAddButton = false,
        HideRemoveButton = false,
        AlwaysExpanded = false,
        Table = true)]

    #endregion

    public List<TableItem> table;

    [ListDrawerSettings(Table = true)]
    public List<TableItem2> table2;

    [Serializable]
    public class TableItem
    {
        [Required]
        public Texture icon;

        public string description;

        [Group("Combined"), LabelWidth(16)]
        public string A, B, C;

        [Button, Group("Actions")]
        public void Test1()
        {
        }

        [Button, Group("Actions")]
        public void Test2()
        {
        }
    }

    [Serializable]
    public class TableItem2
    {
        public bool flag;
        public string text;
        public Vector3 vector;
    }
}