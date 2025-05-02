using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TileRulegenerator : MonoBehaviour {
    public List<Sprite> spriteList;
    public string FileName;

    [ContextMenu("Create Rule Tile from Populated Sprite List")]
    public void CreateRuleTile() {

        Debug.Log("Creating Rule tile.");
        //Add this and spriteList as arguments to the function later.
        RuleTile rTile = ScriptableObject.CreateInstance("RuleTile") as RuleTile;

        if(FileName == "") {
            FileName = "ruleTile";
        }
        AssetDatabase.CreateAsset(rTile, $"Assets/Resources/{FileName}.asset");
        rTile.m_DefaultSprite = spriteList[0];

        //RULE TILES
        //1 = GREEN
        //2 = RED X
        //0 = EMPTY

        rTile.m_DefaultSprite = spriteList[17];

        //RULE r_33 - SE 3Inv
        RuleTile.TilingRule r_33 = new RuleTile.TilingRule();
        r_33.m_Sprites[0] = spriteList[33];
        List<int> r_33_list = new List<int>()
        {
        2,1,2,
        1,  1,
        2,1,1
        };

        r_33.m_ColliderType = UnityEngine.Tilemaps.Tile.ColliderType.Grid;
        r_33.m_Neighbors = r_33_list;
        rTile.m_TilingRules.Add(r_33);


        //RULE r_27 - SW 3Inv
        RuleTile.TilingRule r_27 = new RuleTile.TilingRule();
        r_27.m_Sprites[0] = spriteList[27];
        List<int> r_27_list = new List<int>()
        {
        2,1,2,
        1,  1,
        1,1,2
        };
        r_27.m_Neighbors = r_27_list;
        rTile.m_TilingRules.Add(r_27);

        //RULE r_34 - NW 3Inv
        RuleTile.TilingRule r_34 = new RuleTile.TilingRule();
        r_34.m_Sprites[0] = spriteList[34];
        List<int> r_34_list = new List<int>()
        {
        1,1,2,
        1,  1,
        2,1,2
        };
        r_34.m_Neighbors = r_34_list;
        rTile.m_TilingRules.Add(r_34);

        //RULE r_32 - NE 3Inv
        RuleTile.TilingRule r_32 = new RuleTile.TilingRule();
        r_32.m_Sprites[0] = spriteList[32];
        List<int> r_32_list = new List<int>()
        {
        2,1,1,
        1,  1,
        2,1,2
        };
        r_32.m_Neighbors = r_32_list;
        rTile.m_TilingRules.Add(r_32);

        //RULE r_0 - NW Corner
        RuleTile.TilingRule r_0 = new RuleTile.TilingRule();
        r_0.m_Sprites[0] = spriteList[0];
        List<int> r_0_list = new List<int>()
        {
        0,2,0,
        2,  1,
        0,1,1
        };
        r_0.m_Neighbors = r_0_list;
        rTile.m_TilingRules.Add(r_0);

        //RULE r_02 - NE Corner
        RuleTile.TilingRule r_02 = new RuleTile.TilingRule();
        r_02.m_Sprites[0] = spriteList[2];
        List<int> r_02_list = new List<int>()
        {
        0,2,0,
        1,  2,
        1,1,0
        };
        r_02.m_Neighbors = r_02_list;
        rTile.m_TilingRules.Add(r_02);

        //RULE r_16 - SE Corner
        RuleTile.TilingRule r_16 = new RuleTile.TilingRule();
        r_16.m_Sprites[0] = spriteList[16];
        List<int> r_16_list = new List<int>()
        {
        1,1,0,
        1,  2,
        0,2,0
        };
        r_16.m_Neighbors = r_16_list;
        rTile.m_TilingRules.Add(r_16);

        //RULE r_14 - SW Corner
        RuleTile.TilingRule r_14 = new RuleTile.TilingRule();
        r_14.m_Sprites[0] = spriteList[14];
        List<int> r_14_list = new List<int>()
        {
        0,1,1,
        2,  1,
        0,2,0
        };
        r_14.m_Neighbors = r_14_list;
        rTile.m_TilingRules.Add(r_14);

        //RULE r_01 - N Shore
        RuleTile.TilingRule r_01 = new RuleTile.TilingRule();
        r_01.m_Sprites[0] = spriteList[1];
        List<int> r_01_list = new List<int>()
        {
        0,2,0,
        1,  1,
        1,1,1
        };
        r_01.m_Neighbors = r_01_list;
        rTile.m_TilingRules.Add(r_01);


        //RULE r_43 - N Fat T
        RuleTile.TilingRule r_43 = new RuleTile.TilingRule();
        r_43.m_Sprites[0] = spriteList[43];
        List<int> r_43_list = new List<int>()
        {
        2,1,2,
        1,  1,
        1,1,1
        };
        r_43.m_Neighbors = r_43_list;
        rTile.m_TilingRules.Add(r_43);

        //RULE r_44 - E Fat T 
        RuleTile.TilingRule r_44 = new RuleTile.TilingRule();
        r_44.m_Sprites[0] = spriteList[44];
        List<int> r_44_list = new List<int>()
        {
        1,1,2,
        1,  1,
        1,1,2
        };
        r_44.m_Neighbors = r_44_list;
        rTile.m_TilingRules.Add(r_44);

        //RULE r_45 - S Fat T 
        RuleTile.TilingRule r_45 = new RuleTile.TilingRule();
        r_45.m_Sprites[0] = spriteList[45];
        List<int> r_45_list = new List<int>()
        {
        1,1,1,
        1,  1,
        2,1,2
        };
        r_45.m_Neighbors = r_45_list;
        rTile.m_TilingRules.Add(r_45);

        //RULE r_46 - W Fat T
        RuleTile.TilingRule r_46 = new RuleTile.TilingRule();
        r_46.m_Sprites[0] = spriteList[46];
        List<int> r_46_list = new List<int>()
        {
        2,1,1,
        1,  1,
        2,1,1
        };
        r_46.m_Neighbors = r_46_list;
        rTile.m_TilingRules.Add(r_46);

        //RULE r_03 - NW Invcorner
        RuleTile.TilingRule r_03 = new RuleTile.TilingRule();
        r_03.m_Sprites[0] = spriteList[3];
        List<int> r_03_list = new List<int>()
        {
        1,1,0,
        1,  1,
        0,1,2
        };
        r_03.m_Neighbors = r_03_list;
        rTile.m_TilingRules.Add(r_03);

        //RULE r_04 - NE Invcorner
        RuleTile.TilingRule r_04 = new RuleTile.TilingRule();
        r_04.m_Sprites[0] = spriteList[4];
        List<int> r_04_list = new List<int>()
        {
        0,1,1,
        1,  1,
        2,1,0
        };
        r_04.m_Neighbors = r_04_list;
        rTile.m_TilingRules.Add(r_04);

        //RULE r_05 - NW Elbow
        RuleTile.TilingRule r_05 = new RuleTile.TilingRule();
        r_05.m_Sprites[0] = spriteList[5];
        List<int> r_05_list = new List<int>()
        {
        0,2,0,
        2,  1,
        0,1,2
        };
        r_05.m_Neighbors = r_05_list;
        rTile.m_TilingRules.Add(r_05);

        //RULE r_06 - NE Elbow
        RuleTile.TilingRule r_06 = new RuleTile.TilingRule();
        r_06.m_Sprites[0] = spriteList[6];
        List<int> r_06_list = new List<int>()
        {
        0,2,0,
        1,  2,
        2,1,0
        };
        r_06.m_Neighbors = r_06_list;
        rTile.m_TilingRules.Add(r_06);

        //RULE r_07 - W Shore
        RuleTile.TilingRule r_07 = new RuleTile.TilingRule();
        r_07.m_Sprites[0] = spriteList[7];
        List<int> r_07_list = new List<int>()
        {
        0,1,1,
        2,  1,
        0,1,1
        };
        r_07.m_Neighbors = r_07_list;
        rTile.m_TilingRules.Add(r_07);

        //RULE r_08 - Middle
        RuleTile.TilingRule r_08 = new RuleTile.TilingRule();
        r_08.m_Sprites[0] = spriteList[8];
        List<int> r_08_list = new List<int>()
        {
        1,1,1,
        1,  1,
        1,1,1
        };
        r_08.m_Neighbors = r_08_list;
        rTile.m_TilingRules.Add(r_08);

        //RULE r_09 - E Shore
        RuleTile.TilingRule r_09 = new RuleTile.TilingRule();
        r_09.m_Sprites[0] = spriteList[9];
        List<int> r_09_list = new List<int>()
        {
        1,1,0,
        1,  2,
        1,1,0
        };
        r_09.m_Neighbors = r_09_list;
        rTile.m_TilingRules.Add(r_09);

        //RULE r_10 - SW Inv Corner
        RuleTile.TilingRule r_10 = new RuleTile.TilingRule();
        r_10.m_Sprites[0] = spriteList[10];
        List<int> r_10_list = new List<int>()
        {
        0,1,2,
        1,  1,
        1,1,0
        };
        r_10.m_Neighbors = r_10_list;
        rTile.m_TilingRules.Add(r_10);

        //RULE r_11 - SE Inv Corner
        RuleTile.TilingRule r_11 = new RuleTile.TilingRule();
        r_11.m_Sprites[0] = spriteList[11];
        List<int> r_11_list = new List<int>()
        {
        2,1,0,
        1,  1,
        0,1,1
        };
        r_11.m_Neighbors = r_11_list;
        rTile.m_TilingRules.Add(r_11);

        //RULE r_12 - SW Elbow
        RuleTile.TilingRule r_12 = new RuleTile.TilingRule();
        r_12.m_Sprites[0] = spriteList[12];
        List<int> r_12_list = new List<int>()
        {
        0,1,2,
        2,  1,
        0,2,0
        };
        r_12.m_Neighbors = r_12_list;
        rTile.m_TilingRules.Add(r_12);

        //RULE r_13 - SE Elbow
        RuleTile.TilingRule r_13 = new RuleTile.TilingRule();
        r_13.m_Sprites[0] = spriteList[13];
        List<int> r_13_list = new List<int>()
        {
        2,1,0,
        1,  2,
        0,2,0
        };
        r_13.m_Neighbors = r_13_list;
        rTile.m_TilingRules.Add(r_13);

        //RULE r_15 - S Shore
        RuleTile.TilingRule r_15 = new RuleTile.TilingRule();
        r_15.m_Sprites[0] = spriteList[15];
        List<int> r_15_list = new List<int>()
        {
        1,1,1,
        1,  1,
        0,2,0
        };
        r_15.m_Neighbors = r_15_list;
        rTile.m_TilingRules.Add(r_15);

        //RULE r_17 - Island
        RuleTile.TilingRule r_17 = new RuleTile.TilingRule();
        r_17.m_Sprites[0] = spriteList[17];
        List<int> r_17_list = new List<int>()
        {
        0,2,0,
        2,  2,
        0,2,0
        };
        r_17.m_Neighbors = r_17_list;
        rTile.m_TilingRules.Add(r_17);

        //RULE r_18 - Intersection
        RuleTile.TilingRule r_18 = new RuleTile.TilingRule();
        r_18.m_Sprites[0] = spriteList[18];
        List<int> r_18_list = new List<int>()
        {
        2,1,2,
        1,  1,
        2,1,2
        };
        r_18.m_Neighbors = r_18_list;
        rTile.m_TilingRules.Add(r_18);

        //RULE r_19 - NS Bridge
        RuleTile.TilingRule r_19 = new RuleTile.TilingRule();
        r_19.m_Sprites[0] = spriteList[19];
        List<int> r_19_list = new List<int>()
        {
        0,1,0,
        2,  2,
        0,1,0
        };
        r_19.m_Neighbors = r_19_list;
        rTile.m_TilingRules.Add(r_19);

        //RULE r_20 - WE Bridge
        RuleTile.TilingRule r_20 = new RuleTile.TilingRule();
        r_20.m_Sprites[0] = spriteList[20];
        List<int> r_20_list = new List<int>()
        {
        0,2,0,
        1,  1,
        0,2,0
        };
        r_20.m_Neighbors = r_20_list;
        rTile.m_TilingRules.Add(r_20);

        //RULE r_21 - W End
        RuleTile.TilingRule r_21 = new RuleTile.TilingRule();
        r_21.m_Sprites[0] = spriteList[21];
        List<int> r_21_list = new List<int>()
        {
        0,2,0,
        2,  1,
        0,2,0
        };
        r_21.m_Neighbors = r_21_list;
        rTile.m_TilingRules.Add(r_21);

        //RULE r_22 - E End
        RuleTile.TilingRule r_22 = new RuleTile.TilingRule();
        r_22.m_Sprites[0] = spriteList[23];
        List<int> r_22_list = new List<int>()
        {
        0,2,0,
        1,  2,
        0,2,0
        };
        r_22.m_Neighbors = r_22_list;
        rTile.m_TilingRules.Add(r_22);

        //RULE r_24 - S End
        RuleTile.TilingRule r_24 = new RuleTile.TilingRule();
        r_24.m_Sprites[0] = spriteList[24];
        List<int> r_24_list = new List<int>()
        {
        0,1,0,
        2,  2,
        0,2,0
        };
        r_24.m_Neighbors = r_24_list;
        rTile.m_TilingRules.Add(r_24);

        //RULE r_23 - N End 
        RuleTile.TilingRule r_23 = new RuleTile.TilingRule();
        r_23.m_Sprites[0] = spriteList[22];
        List<int> r_23_list = new List<int>()
        {
        0,2,0,
        2,  2,
        0,1,0
        };
        r_23.m_Neighbors = r_23_list;
        rTile.m_TilingRules.Add(r_23);


        //RULE r_25 - SW NE Bridge
        RuleTile.TilingRule r_25 = new RuleTile.TilingRule();
        r_25.m_Sprites[0] = spriteList[25];
        List<int> r_25_list = new List<int>()
        {
        2,1,1,
        1,  1,
        1,1,2
        };
        r_25.m_Neighbors = r_25_list;
        rTile.m_TilingRules.Add(r_25);

        //RULE r_26 - NW SE Bridge
        RuleTile.TilingRule r_26 = new RuleTile.TilingRule();
        r_26.m_Sprites[0] = spriteList[26];
        List<int> r_26_list = new List<int>()
        {
        1,1,2,
        1,  1,
        2,1,1
        };
        r_26.m_Neighbors = r_26_list;
        rTile.m_TilingRules.Add(r_26);

        //RULE r_28 - N T
        RuleTile.TilingRule r_28 = new RuleTile.TilingRule();
        r_28.m_Sprites[0] = spriteList[28];
        List<int> r_28_list = new List<int>()
        {
        2,1,2,
        1,  1,
        0,2,0
        };
        r_28.m_Neighbors = r_28_list;
        rTile.m_TilingRules.Add(r_28);

        //RULE r_29 - E T
        RuleTile.TilingRule r_29 = new RuleTile.TilingRule();
        r_29.m_Sprites[0] = spriteList[29];
        List<int> r_29_list = new List<int>()
        {
        0,1,2,
        2,  1,
        0,1,2
        };
        r_29.m_Neighbors = r_29_list;
        rTile.m_TilingRules.Add(r_29);

        //RULE r_30 - S T
        RuleTile.TilingRule r_30 = new RuleTile.TilingRule();
        r_30.m_Sprites[0] = spriteList[30];
        List<int> r_30_list = new List<int>()
        {
        0,2,0,
        1,  1,
        2,1,2
        };
        r_30.m_Neighbors = r_30_list;
        rTile.m_TilingRules.Add(r_30);

        //RULE r_31 - W T
        RuleTile.TilingRule r_31 = new RuleTile.TilingRule();
        r_31.m_Sprites[0] = spriteList[31];
        List<int> r_31_list = new List<int>()
        {
        2,1,0,
        1,  2,
        2,1,0
        };
        r_31.m_Neighbors = r_31_list;
        rTile.m_TilingRules.Add(r_31);

        //RULE r_35 - N GUN
        RuleTile.TilingRule r_35 = new RuleTile.TilingRule();
        r_35.m_Sprites[0] = spriteList[35];
        List<int> r_35_list = new List<int>()
        {
        0,1,2,
        2,  1,
        0,1,1
        };
        r_35.m_Neighbors = r_35_list;
        rTile.m_TilingRules.Add(r_35);

        //RULE r_36 - E Gun
        RuleTile.TilingRule r_36 = new RuleTile.TilingRule();
        r_36.m_Sprites[0] = spriteList[36];
        List<int> r_36_list = new List<int>()
        {
        0,2,0,
        1,  1,
        1,1,2
        };
        r_36.m_Neighbors = r_36_list;
        rTile.m_TilingRules.Add(r_36);

        //RULE r_37 - S Gun
        RuleTile.TilingRule r_37 = new RuleTile.TilingRule();
        r_37.m_Sprites[0] = spriteList[37];
        List<int> r_37_list = new List<int>()
        {
        1,1,0,
        1,  2,
        2,1,0
        };
        r_37.m_Neighbors = r_37_list;
        rTile.m_TilingRules.Add(r_37);

        //RULE r_38 - W Gun
        RuleTile.TilingRule r_38 = new RuleTile.TilingRule();
        r_38.m_Sprites[0] = spriteList[38];
        List<int> r_38_list = new List<int>()
        {
        2,1,1,
        1,  1,
        0,2,0
        };
        r_38.m_Neighbors = r_38_list;
        rTile.m_TilingRules.Add(r_38);

        //RULE r_39 - N InvGun
        RuleTile.TilingRule r_39 = new RuleTile.TilingRule();
        r_39.m_Sprites[0] = spriteList[39];
        List<int> r_39_list = new List<int>()
        {
        2,1,0,
        1,  2,
        1,1,0
        };
        r_39.m_Neighbors = r_39_list;
        rTile.m_TilingRules.Add(r_39);

        //RULE r_40 - E InvGun
        RuleTile.TilingRule r_40 = new RuleTile.TilingRule();
        r_40.m_Sprites[0] = spriteList[40];
        List<int> r_40_list = new List<int>()
        {
        1,1,2,
        1,  1,
        0,2,0
        };
        r_40.m_Neighbors = r_40_list;
        rTile.m_TilingRules.Add(r_40);

        //RULE r_41 - S InvGun
        RuleTile.TilingRule r_41 = new RuleTile.TilingRule();
        r_41.m_Sprites[0] = spriteList[41];
        List<int> r_41_list = new List<int>()
        {
        0,1,1,
        2,  1,
        0,1,2
        };
        r_41.m_Neighbors = r_41_list;
        rTile.m_TilingRules.Add(r_41);


        //RULE r_42 - W InvGun
        RuleTile.TilingRule r_42 = new RuleTile.TilingRule();
        r_42.m_Sprites[0] = spriteList[42];
        List<int> r_42_list = new List<int>()
        {
        0,2,0,
        1,  1,
        2,1,1
        };
        r_42.m_Neighbors = r_42_list;
        rTile.m_TilingRules.Add(r_42);
        Debug.Log($"File created at: Assets/Resources/{FileName}.asset");

        //Makes it so that when you leave playmode, the rule tile stays with its settings
        EditorUtility.SetDirty(rTile);
    }

}