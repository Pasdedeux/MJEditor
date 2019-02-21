using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using LitFramework.LitTool;
using System.Text;
using LitJson;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;


[ExecuteInEditMode]
public class MapEditor : EditorWindow
{
    //  开启一个窗口
    [MenuItem( "MapEditor/Open" )]
    static void Open()
    {
        GetWindow<MapEditor>();
    }


    private void Awake()
    {
        SceneView.onSceneGUIDelegate += DelOnSceneGUI;
    }


    private void OnDestroy()
    {
        SceneView.onSceneGUIDelegate -= DelOnSceneGUI;
    }

    private void DelOnSceneGUI( SceneView sceneView )
    {
        if ( Event.current.type == EventType.MouseDown )
        {
            if ( Event.current.button == 0 )
            {
                RaycastHit hit;
                Ray ray = HandleUtility.GUIPointToWorldRay( Event.current.mousePosition );
                //UnityEngine.Debug.DrawRay( ray.origin, ray.direction, Color.blue, 100f );
                if ( Physics.Raycast( ray, out hit ) )
                {
                    UnityEngine.Debug.Log( hit.collider.gameObject.name );
                    string[] nameList = hit.collider.gameObject.name.Split( '_' );
                    int layer = int.Parse( nameList[ 0 ] );
                    int row = int.Parse( nameList[ 1 ] );
                    int col = int.Parse( nameList[ 2 ] );
                    int index = int.Parse( nameList[ 3 ] );
                    
                    //非边界点
                    if( row>0 && row <RowNum-1 && col >0 && col <ColNum-1 )
                        RealAddCard( layer, index );
                }
                UnityEngine.Debug.Log( "Left-Mouse Down" );
            }
            else if ( Event.current.button == 1 )
            {
                RaycastHit hit;
                Ray ray = HandleUtility.GUIPointToWorldRay( Event.current.mousePosition );
                if ( Physics.Raycast( ray, out hit ) )
                {
                    string[] nameList = hit.collider.gameObject.name.Split( '_' );
                    int layer = int.Parse( nameList[ 0 ] );
                    int row = int.Parse( nameList[ 1 ] );
                    int col = int.Parse( nameList[ 2 ] );
                    int index = int.Parse( nameList[ 3 ] );

                    //必须是核心点
                    if ( _allTransformLayers[ layer ][ index ].GetComponent<SpriteRenderer>().color == Color.red )
                        RealRemoveCard( layer, index, row, col );
                }
                UnityEngine.Debug.Log( "Right-Mouse Down" );
            }
        }

    }


    private void RealAddCard(int layer,int index)
    {
        //9点是否可用
        int[] targetIndexs = new int[ 9 ];

        var row = Mathf.FloorToInt( ( float )index / ColNum );
        var col = index % ColNum;

        targetIndexs[ 0 ] = ( row - 1 ) * ColNum + col - 1;
        targetIndexs[ 1 ] = row * ColNum + col - 1;
        targetIndexs[ 2 ] = ( row + 1 ) * ColNum + col - 1;
        targetIndexs[ 3 ] = ( row - 1 ) * ColNum + col;

        targetIndexs[ 4 ] = ( row - 1 ) * ColNum + col + 1;
        targetIndexs[ 5 ] = row * ColNum + col + 1;
        targetIndexs[ 6 ] = ( row + 1 ) * ColNum + col + 1;
        targetIndexs[ 7 ] = ( row + 1 ) * ColNum + col;
        targetIndexs[ 8 ] = index;

        //bool result = true;
        //for ( int q = 0; q < targetIndexs.Length; q++ )
        //{
        //    if ( _allIndexLayers[ layer ][ targetIndexs[ q ] ] > 0 )
        //    {
        //        result = false;
        //        break;
        //    }
        //}

        if ( _allIndexLayers[layer][index]<1 )
        {
            for ( int i = 0; i < targetIndexs.Length; i++ )
            {
                AddOccupyIndex( targetIndexs[ i ], layer );

                if ( i == targetIndexs.Length - 1 )
                    _allTransformLayers[ layer ][ targetIndexs[ i ] ].GetComponent<SpriteRenderer>().color = Color.red;
                else
                    _allTransformLayers[ layer ][ targetIndexs[ i ] ].GetComponent<SpriteRenderer>().color = Color.yellow;
            }
            _currentLevelCoreIndex[ layer ].Add( index );

            CurrentUsedCardNum++;
            EditorUpdateLabel();
        }
    }


    private void RealRemoveCard(int layer, int index, int row, int col)
    {
        //9点是否可用
        int[] targetIndexs = new int[ 9 ];

        targetIndexs[ 0 ] = ( row - 1 ) * ColNum + col - 1;
        targetIndexs[ 1 ] = row * ColNum + col - 1;
        targetIndexs[ 2 ] = ( row + 1 ) * ColNum + col - 1;
        targetIndexs[ 3 ] = ( row - 1 ) * ColNum + col;

        targetIndexs[ 4 ] = ( row - 1 ) * ColNum + col + 1;
        targetIndexs[ 5 ] = row * ColNum + col + 1;
        targetIndexs[ 6 ] = ( row + 1 ) * ColNum + col + 1;
        targetIndexs[ 7 ] = ( row + 1 ) * ColNum + col;
        targetIndexs[ 8 ] = index;


        for ( int i = 0; i < targetIndexs.Length; i++ )
        {
            RemoveOccupyIndex( targetIndexs[ i ], layer );

            if ( _allIndexLayers[ layer ][ targetIndexs[ i ] ] == 0 )
            {
                _allTransformLayers[ layer ][ targetIndexs[ i ] ].GetComponent<SpriteRenderer>().color = Color.white;
            }
        }
        _currentLevelCoreIndex[ layer ].Remove( index );

        CurrentUsedCardNum--;
        EditorUpdateLabel();
    }

    //外向输出参数
    public string CurrentLevelID = "0";
    public int RowNum { get; set; }
    public int ColNum { get; set; }
    public int ColorNum { get; set; }
    public int FlowerNum { get; set; }
    public int CanPairNum { get; set; }
    public int CurrentUsedCardNum { get; set; }
    public int FullStarTime { get; set; }
    public Vector2 GridOffset { get; set; }
    public List<bool> LayerList = new List<bool>();

    private GameObject _scene;
    private Dictionary<int, List<int>> _currentLevelCoreIndex = new Dictionary<int, List<int>>();
    //场景层节点
    private List<Transform> _allSceneLayers = new List<Transform>();
    //每层全矩阵字典
    private Dictionary<int, Dictionary<int, int>> _allIndexLayers = new Dictionary<int, Dictionary<int, int>>();
    //每层全矩阵预制件
    private Dictionary<int, Dictionary<int, Transform>> _allTransformLayers = new Dictionary<int, Dictionary<int, Transform>>();
    //float angle = 0;
    //bool on, one, two, three;

    //int selected;
    //GameObject _go;
    //Material mat;
    //Texture texture;

    private int _row, _col;
    void OnGUI()
    {
        //创建object选择控件 GameObject,Material,AudioClip
        //_go = EditorGUILayout.ObjectField( _go, typeof( GameObject ), false ) as GameObject;

        //mat = EditorGUILayout.ObjectField(mat, typeof(Material), false) as Material;

        //EditorGUILayout.ObjectField(null, typeof(AudioClip), false);

        ////      做一个固定大小的Layout
        //var options = new[] { GUILayout.Width(64), GUILayout.Height(64) };

        //texture = EditorGUILayout.ObjectField(texture, typeof(Texture), false, options) as Texture;

        //EditorGUILayout.ObjectField(null, typeof(Sprite), false, options);

        //      创建一个带有层级关系的控件
        //EditorGUILayout.LabelField("Parent");

        //EditorGUI.indentLevel++;
        //for (int i = 0; i < 3; i++)
        //{
        //    EditorGUILayout.LabelField("Child" + i);
        //    //EditorGUI.indentLevel++;
        //}


        //EditorGUI.indentLevel--;

        //EditorGUILayout.LabelField("Parent");

        //EditorGUI.indentLevel++;
        //for (int i = 0; i < 3; i++)
        //{
        //    EditorGUILayout.LabelField("Child" + i);
        //    //EditorGUI.indentLevel--;
        //}



        //创建一个横向排列的按钮
        using ( new EditorGUILayout.VerticalScope() )
        {
            using ( new BackgroundColorScope( Color.white ) )
            {
                if ( GUILayout.Button( "保存关卡" ) )
                {
                    if ( int.Parse( CurrentLevelID ) < 1 )
                    {
                        EditorUtility.DisplayDialog( "提醒", string.Format( "关卡ID需要>0。目前是：{0}", CurrentLevelID ), "确定" );
                        return;
                    }

                    SaveFile();
                    EditorUtility.DisplayDialog( "提醒", string.Format( "关卡 {0} 配置已保存", CurrentLevelID ), "确定" );
                }
            }

            using ( new BackgroundColorScope( Color.white ) )
            {
                if ( GUILayout.Button( "读取关卡" ) )
                {
                    if ( int.Parse( CurrentLevelID ) < 1 )
                    {
                        EditorUtility.DisplayDialog( "提醒", string.Format( "关卡ID需要>0。目前是：{0}", CurrentLevelID ), "确定" );
                        return;
                    }

                    LoadFile();
                }
            }

            using ( new BackgroundColorScope( Color.green ) )
            {
                if ( GUILayout.Button( "清除关卡" ) )
                {
                    WipeData();
                }
            }

            GUILayout.Label( "关卡ID" );
            CurrentLevelID = GUILayout.TextField( CurrentLevelID );
            _row = EditorGUILayout.IntField( "关卡纵向最大牌数", _row );
            _col = EditorGUILayout.IntField( "关卡横向最大牌数", _col );
            GridOffset = EditorGUILayout.Vector2Field( "当前关卡网格偏移", GridOffset );
            ColorNum = EditorGUILayout.IntField( "花色数", ColorNum );
            FlowerNum = EditorGUILayout.IntField( "花牌数", FlowerNum );
            CanPairNum = EditorGUILayout.IntField( "参与可选位牌数", CanPairNum );
            FullStarTime = EditorGUILayout.IntField( "三星通关时间（秒）", FullStarTime );
            EditorGUILayout.LabelField( "当前关卡已指定牌数", CurrentUsedCardNum.ToString() );

            ColNum = 1 + 2 * _col;
            RowNum = 1 + 2 * _row;

            if ( _scene == null ) _scene = GameObject.Find( "Scene" );
            if ( _scene != null ) _scene.transform.position = GridOffset;

            using ( new EditorGUILayout.HorizontalScope() )
            {
                //设置按钮颜色
                using ( new BackgroundColorScope( Color.white ) )
                {
                    if ( GUILayout.Button( "生成一层矩阵" ) )
                    {
                        LayerList.Add( true );
                        CreateLayer( LayerList.Count - 1 );
                    }
                }
                using ( new BackgroundColorScope( Color.red ) )
                {
                    if ( GUILayout.Button( "移除最后矩阵" ) )
                    {
                        if ( LayerList.Count > 0 )
                        {
                            RemoveLayer( LayerList.Count - 1 );
                            LayerList.RemoveAt( LayerList.Count - 1 );
                        }
                    }
                }
            }
        }

        ////      创建一个toggle
        //on = GUILayout.Toggle(on, on ? "on" : "off", "button");
        //      创建一个拉动旋钮
        //angle = EditorGUILayout.Knob(Vector2.one * 100,
        //angle, 0, 360, "度>>上下拉动旋钮", Color.blue, Color.red, true);

        using ( new EditorGUILayout.VerticalScope() )
        {
            //复选框
            using ( new EditorGUILayout.HorizontalScope() )
            {
                for ( int i = 0; i < LayerList.Count; i++ )
                {
                    var newChecked = GUILayout.Toggle( LayerList[ i ], ( i + 1 ).ToString(), EditorStyles.miniButton );
                    if ( newChecked != LayerList[ i ] ) ShowLayer( i, newChecked );
                    LayerList[ i ] = newChecked;
                }
            }
        }


        using ( new BackgroundColorScope( Color.gray ) )
        {
            if ( GUILayout.Button( "修复点击" ) )
            {
                SceneView.onSceneGUIDelegate = null;
                SceneView.onSceneGUIDelegate += DelOnSceneGUI;
            }
        }
    }

    private void EditorUpdateLabel()
    {
        
    }

    private void WipeData()
    {
        _scene = GameObject.Find( "Scene" );
        if ( _scene != null ) Editor.DestroyImmediate( _scene );
        _scene = new GameObject( "Scene" );

        CurrentLevelID = "0";
        RowNum = 0;
        ColNum = 0;
        ColorNum = 0;
        FlowerNum = 0;
        CanPairNum = 0;
        CurrentUsedCardNum = 0;
        FullStarTime = 0;
        GridOffset = Vector2.zero;
        LayerList.Clear();

        _col = _row = 0;
        _allSceneLayers.Clear();
        _allIndexLayers.Clear();
        _allTransformLayers.Clear();
        _currentLevelCoreIndex.Clear();
        GC.Collect();
    }
    
    #region 逻辑输出


    #region 保存

    /// <summary>
    /// 保存配置
    /// </summary>
    private void SaveFile()
    {
        if ( CurrentUsedCardNum>140 )
        {
            EditorUtility.DisplayDialog( "提醒", string.Format( "已超过麻将数上限：140，当前使用 {0}", CurrentUsedCardNum ), "确定" );
            return;
        }
        var filepath = AssetPathManager.Instance.GetStreamAssetDataPath( string.Format( "level_{0}.dat", CurrentLevelID ) );

        FileInfo fileInfo = new FileInfo(filepath);
        LevelData leveldata = new LevelData();
        leveldata.LevelID = int.Parse( CurrentLevelID );
        leveldata.LayerNum = LayerList.Count;
        leveldata.ColorNum = ColorNum;
        leveldata.FlowerNum = FlowerNum;
        leveldata.CanPairNum = CanPairNum;
        leveldata.GridOffsetX = GridOffset.x;
        leveldata.GridOffsetY = GridOffset.y;
        leveldata.RowNum = RowNum;
        leveldata.ColNum = ColNum;
        leveldata.FullStarTime = FullStarTime;

        //todo=============================== Dict 存储
        //LayerData layerData = new LayerData();
        //layerData.LayerID = 0;
        //layerData.LayerIndexList = new List<int>();

        //leveldata.IndexListPerLayerList.Add( new LayerData() { LayerID = 2, LayerIndexList = new List<int> { 10, 13 } } );
        //leveldata.IndexListPerLayerList.Add( new LayerData() { LayerID = 1, LayerIndexList = new List<int> { 16, 19 } } );
        //leveldata.IndexListPerLayerList.Add( new LayerData() { LayerID = 0, LayerIndexList = new List<int> { 16 } } );
        //todo=============================== Dict 存储

        foreach ( var item in _currentLevelCoreIndex )
        {
            LayerData layerData = new LayerData();
            layerData.LayerID = item.Key;
            layerData.LayerIndexList = new List<int>();
            layerData.LayerIndexList.AddRange( item.Value );
            leveldata.IndexListPerLayerList.Add( layerData );
        }

        //sorting 
        leveldata.IndexListPerLayerList.Sort( ( a, b ) => { return a.LayerID < b.LayerID ? -1 : 1; } );

        using ( StreamWriter sw = fileInfo.CreateText() )
        {
            var jsonText = JsonMapper.ToJson( leveldata );
            sw.Write( jsonText );
        }

        UpdateAllLevels( CurrentLevelID );
    }

    private static void UpdateAllLevels( string levelID )
    {
        string filepath = AssetPathManager.Instance.GetStreamAssetDataPath( "levels.dat" );
        FileInfo fileInfo = new FileInfo( filepath );

        StreamWriter sw;
        StringBuilder sb = new StringBuilder();
        if ( !File.Exists( filepath ) )
        {
            sw = fileInfo.CreateText();
            sb.Append( "Level_" + levelID );
        }
        else
        {
            List<string> levels;
            using ( var fs = fileInfo.OpenText() )
            {
                string allLines = fs.ReadToEnd();
                allLines = Regex.Replace( allLines, "[\f\n\r\t\v]", "" );
                levels = allLines.Split( '|' ).ToList();
                if ( !levels.Contains( "Level_" + levelID ) )
                {
                    levels.Add( "Level_" + levelID );
                    levels.Sort();
                }
            }

            sw = new StreamWriter( filepath );
            sb.Append( string.Join( "|", levels.ToArray() ) );
        }

        sw.WriteLine( sb.ToString() );
        sw.Close();
    }


    #endregion

    #region 读取

    /// <summary>
    /// 读取配置
    /// </summary>
    private void LoadFile()
    {
        var filepath = AssetPathManager.Instance.GetStreamAssetDataPath( string.Format( "level_{0}.dat", CurrentLevelID ) );
        FileInfo fileInfo = new FileInfo( filepath );
        if ( !File.Exists( filepath ) )
        {
            EditorUtility.DisplayDialog( "配置不存在", "关卡ID尚未配置", "确定" );
            return;
        }

        WipeData();

        LevelData levelData;
        using ( var fs = fileInfo.OpenText() )
            levelData = JsonMapper.ToObject<LevelData>( fs.ReadToEnd() );

        CurrentLevelID = levelData.LevelID.ToString();
        RowNum = levelData.RowNum;
        ColNum = levelData.ColNum;
        ColorNum = levelData.ColorNum;
        FlowerNum = levelData.FlowerNum;
        CanPairNum = levelData.CanPairNum;
        FullStarTime = levelData.FullStarTime;
        GridOffset = new Vector2( ( float )levelData.GridOffsetX, ( float )levelData.GridOffsetY );

        _col = (ColNum - 1) / 2;
        _row = ( RowNum - 1 ) / 2;
        _scene.transform.position = GridOffset;

        var list = levelData.IndexListPerLayerList;
        for ( int i = 0; i < list.Count; i++ )
        {
            LayerList.Add( true );
            //CurrentUsedCardNum += list[ i ].LayerIndexList.Count;
            CreateLayer( list[ i ].LayerID );
            
            for ( int q = 0; q < list[ i ].LayerIndexList.Count; q++ )
                RealAddCard( list[ i ].LayerID, list[ i ].LayerIndexList[ q ] );
        }
    }

    #endregion

    private static float CARDS_HALFWIDTH = 114 * 0.5f * 0.01f;
    private static float CARDS_HALFHEIGHT = 152 * 0.5f * 0.01f;
    /// <summary>
    /// 增加层
    /// </summary>
    /// <param name="layerIndex">Layer index.</param>
    private void CreateLayer( int layerIndex )
    {
        //汇总数据
        if ( !_currentLevelCoreIndex.ContainsKey( layerIndex ) )
            _currentLevelCoreIndex.Add( layerIndex, new List<int>() );

        //点位数据
        GenerateMapGrid( layerIndex );

        var layerList = _allIndexLayers[ layerIndex ];
        var newSceneLayer = new GameObject( layerIndex.ToString() );
        newSceneLayer.transform.SetParent( _scene.transform );
        newSceneLayer.transform.localPosition = -Vector3.forward * layerIndex;
        newSceneLayer.transform.localScale = Vector3.one;

        _allSceneLayers.Add( newSceneLayer.transform );
        _allTransformLayers.Add( layerIndex, new Dictionary<int, Transform>() );
        foreach ( var item in layerList.Keys )
        {
            GameObject grid = GameObject.Instantiate( AssetDatabase.LoadAssetAtPath( "Assets/EditorSource/New Sprite.prefab", typeof( GameObject ) ) as GameObject );
            grid.transform.SetParent( newSceneLayer.transform );

            float xCol = item % ColNum;
            float yRow = Mathf.FloorToInt( ( float )item / ColNum );

            Vector3 pos = new Vector3( xCol * CARDS_HALFWIDTH * 2 / 3, yRow * CARDS_HALFHEIGHT * 2 / 3 * -1, 0 );
            grid.transform.localPosition = pos;
            grid.transform.name = layerIndex + "_" + yRow + "_" + xCol + "_" + item;

            _allTransformLayers[ layerIndex ].Add( item, grid.transform );
        }
    }

    /// <summary>
    /// 删除指定层
    /// </summary>
    /// <param name="layerIndex">Layer index.</param>
    private void RemoveLayer( int layerIndex )
    {
        if ( _allSceneLayers.Count - 1 >= layerIndex )
        {
            GameObject.DestroyImmediate( _allSceneLayers[ layerIndex ].gameObject );
            _allSceneLayers.RemoveAt( layerIndex );
        }
        _allTransformLayers.Remove( layerIndex );

        //点位数据
        DelteMapGrid( layerIndex );

        //汇总数据
        if ( _currentLevelCoreIndex.ContainsKey( layerIndex ) )
        {
            _currentLevelCoreIndex[ layerIndex ].Clear();
            _currentLevelCoreIndex.Remove( layerIndex );
        }
    }

    /// <summary>
    /// 展示层
    /// </summary>
    /// <param name="i">The index.</param>
    /// <param name="show">If set to <c>true</c> v.</param>
    private void ShowLayer( int i, bool show )
    {
        Debug.Log( string.Format( "<color=red>{0}/{1}</color>", i, show ) );
        //_layer[i]层显
        _allSceneLayers[ i ].gameObject.SetActive( show );
    }

    private void GenerateMapGrid( int layer )
    {
        int index = 0;
        for ( int i = 0; i < RowNum; i++ )
        {
            for ( int k = 0; k < ColNum; k++ )
            {
                AddOccupyIndex( index, layer );
                index++;
            }
        }
    }
    private void DelteMapGrid( int layer )
    {
        if ( _allIndexLayers.ContainsKey( layer ) )
        {
            _allIndexLayers.Remove( layer );
        }
    }
    private void AddOccupyIndex( int index, int layerIndex )
    {
        if ( !_allIndexLayers.ContainsKey( layerIndex ) )
            _allIndexLayers.Add( layerIndex, new Dictionary<int, int>() );

        if ( !_allIndexLayers[ layerIndex ].ContainsKey( index ) )
            _allIndexLayers[ layerIndex ].Add( index, 0 );
        else
            _allIndexLayers[ layerIndex ][ index ]++;
    }
    private void RemoveOccupyIndex( int index, int layerIndex )
    {
        //移动到边界处于可撤销状态时，设边界索引值为-1，同时视为标记
        if ( index < 0 ) return;
        try
        {
            _allIndexLayers[ layerIndex ][ index ] = Mathf.Max( 0, _allIndexLayers[ layerIndex ][ index ] - 1 );
        }
        catch ( Exception ex )
        {
            throw new Exception( string.Format( "Point Index Error: layerIndex>{0}:index>{1}-{2} ", layerIndex, index, ex ) );
        }
    }
    #endregion


    /// <summary>
    /// 关卡数据结构
    /// </summary>
    public class LevelData
    {
        public int LevelID;
        public int RowNum;
        public int ColNum;
        public int LayerNum;
        public int ColorNum;
        public int FlowerNum;
        public int CanPairNum;
        public double GridOffsetX;
        public double GridOffsetY;
        public int FullStarTime;

        //字典存储各层Index信息
        public List<LayerData> IndexListPerLayerList = new List<LayerData>();
    }

    public class LayerData
    {
        public int LayerID;
        public List<int> LayerIndexList = new List<int>();
    }
}

#region 颜色

public class BackgroundColorScope : GUI.Scope
{
    private readonly Color color;
    public BackgroundColorScope( Color color )
    {
        this.color = GUI.backgroundColor;
        GUI.backgroundColor = color;
    }


    protected override void CloseScope()
    {
        GUI.backgroundColor = Color.cyan;
    }
    //    public Color sideColor = Color.white;
    //    public Color insideColor = Color.black;

    //    public float width = 5f;
    //    public float height = 1.0f;

    //    public int mapHeight = 10;
    //    public int mapWidth = 15;

    //    // Use this for initialization
    //    void Start()
    //    {

    //    }

    //    void OnDrawGizmos()
    //    {
    //        DrawBGLine();
    //    }

    //    public void DrawBGLine()
    //    {
    //#if UNITY_EDITOR

    //        //Gizmos.color = insideColor;

    //        //for (float y = 0; y < mapHeight; y += height)
    //        //{
    //        //    Gizmos.DrawLine(new Vector3(0, y, -1f), new Vector3(mapWidth, y, -1f));

    //        //    Handles.Label(new Vector3(-1f, y + 0.5f, 0f), "" + y);
    //        //}

    //        //for (float x = 0; x < mapWidth; x += width)
    //        //{
    //        //    Gizmos.DrawLine(new Vector3(x, 0, -1f), new Vector3(x, mapHeight, -1f));

    //        //    Handles.Label(new Vector3(x, -0.2f, 0f), "" + x);
    //        //}

    //        //Gizmos.color = sideColor;

    //        //Gizmos.DrawLine(new Vector3(0, 0, -1f), new Vector3(mapWidth, 0, -1f));
    //        //Gizmos.DrawLine(new Vector3(mapWidth, 0, -1f), new Vector3(mapWidth, mapHeight, -1f));
    //        //Gizmos.DrawLine(new Vector3(mapWidth, mapHeight, -1f), new Vector3(0, mapHeight, -1f));
    //        //Gizmos.DrawLine(new Vector3(0, mapHeight, -1f), new Vector3(0, 0, -1f));

    //#endif
    //}

    #endregion
}
#endif