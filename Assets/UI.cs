using System;
using UnityEngine;
using UnityEngine.UI;

namespace TaiwuEditor
{
    internal class UI : MonoBehaviour
    {
        #region Constant
        /// <summary>窗口背景图</summary>
        private const string windowBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAEACAYAAACZCaebAAAAnElEQVRIS63MtQHDQADAwPdEZmaG/fdJCq2g7qqLvu/7hRBCZOF9X0ILz/MQWrjvm1DHdV3MFs7zJLRwHAehhX3fCS1s20ZoYV1XQgvLshDqmOeZ2cI0TYQWxnEktDAMA6GFvu8JLXRdR2ihbVtCHU3TMFuo65rQQlVVhBbKsiS0UBQFoYU8zwktZFlGqCNNU2YLSZIQWojjmFDCH22GtZAncD8TAAAAAElFTkSuQmCC";
        /// <summary>窗口宽度基准</summary>
        private const float windowWidthBase = 960f;
        /// <summary>调整入邪值按钮基准宽度</summary>
        private const float evilButtonWidthBase = 120f;
        /// <summary>调整疗伤祛毒理气按钮基准宽度</summary>
        private const float healthButtonWidthBase = 150f;
        /// <summary>人物属性修改页面中，人物id字段显示基准宽度</summary>
        private const float idFieldWidthBase = 100f;
        /// <summary>界面字体基准大小</summary>
        private const int normalFontSizeBase = 16;
        /// <summary>关闭按钮的基准长度</summary>
        private const float closeBtnWidthBase = 150f;
        /// <summary>修改框标签基准宽度</summary>
        private const float fieldHelperBtnWidthBase = 75f;
        /// <summary>修改框基准宽度</summary>
        private const float fieldHelperLblWidthBase = 90f;
        /// <summary>修改框修改按钮基准宽度</summary>
        private const float fieldHelperTextWidthBase = 125f;
        /// <summary>功能选项卡</summary>
        private static readonly string[] funcTab =
        {
            "<color=#FFA500>基本功能</color>",
            "<color=#FFA500>属性修改</color>",
            "<color=#FFA500>功法</color>"
        };
        /// <summary>属性资源修改选项卡</summary>
        private static readonly string[] dataTabNames =
        {
            "<color=#87CEEB>基本属性：</color>",
            "<color=#87CEEB>资源： </color>",
            "<color=#87CEEB>技艺资质： </color>",
            "<color=#87CEEB>功法资质： </color>",
            "<color=#87CEEB>太吾本人限定： </color>",
            "<color=#87CEEB>健康、寿命： </color>",
            "<color=#87CEEB>相枢入邪值修改：</color><color=#FFA500>(对“执迷入邪” 和 “执迷化魔” 无效，这两者只与奇书功法学习进度有关)</color>",
            //"Test"
        };
        #endregion

        #region Style
        /// <summary>窗口的样式</summary>
        public static GUIStyle windowStyle;
        /// <summary>修改器背景贴图</summary>
        private static Texture2D windowTexture;
        /// <summary>标题的样式</summary>
        public static GUIStyle titleStyle;
        /// <summary>修改人物属性界面样式</summary>
        public static GUIStyle propertiesUIButtonStyle;
        /// <summary>标签的样式</summary>
        public static GUIStyle labelStyle;
        /// <summary>人物姓名显示的样式</summary>
        public static GUIStyle nameFieldStyle;
        /// <summary>按钮样式</summary>
        public static GUIStyle buttonStyle;
        /// <summary>选择框的样式</summary>
        public static GUIStyle toggleStyle;
        /// <summary>box组件的样式</summary>
        public static GUIStyle boxStyle;
        /// <summary>输入框的样式</summary>
        public static GUIStyle textFieldStyle;
        /// <summary>说明文字的样式</summary>
        public static GUIStyle commentStyle;
        #endregion

        #region Instance
        public static UI Instance { get; private set; }
        #endregion

        /// <summary>太吾修改器的参数</summary>
        //private static Settings modSettings;

        #region Private Class Member
        /// <summary>UI是否已初始化</summary>
        private bool mInit;
        /// <summary>主窗口尺寸</summary>
        private Rect mWindowRect = new Rect(0f, 0f, 0f, 0f);
        /// <summary>主窗口最小宽度</summary>
        private float mWindowWidth = windowWidthBase;
        /// <summary>调整入魔值按钮的宽度</summary>
        private float evilButtonWidth = evilButtonWidthBase;
        /// <summary>调整疗伤祛毒理气按钮宽度</summary>
        private float healthButtonWidth = healthButtonWidthBase;
        /// <summary>人物属性修改页面中，人物id字段显示宽度</summary>
        private float idFieldWidth = idFieldWidthBase;
        /// <summary>界面字体大小</summary>
        private int normalFontSize = normalFontSizeBase;
        /// <summary>关闭按钮的长度</summary>
        private float closeBtnWidth = closeBtnWidthBase;
        /// <summary>屏蔽游戏界面的幕布</summary>
        private GameObject mCanvas;
        /// <summary>功能选择，0是基本功能，1是修改属性</summary>
        private int funcChoose = 0;
        /// <summary>选择修改哪个人物的属性，0太吾，1上一个打开菜单的人物，2自定义人物</summary>
        private int basePropertyChoose = 0;
        /// <summary>显示在自定义人物ID输入框中的值</summary>
        private string displayedActorId = "0";
        /// <summary>想要修改属性的NPC ID</summary>
        private int actorId = 0;
        /// <summary>滚动条的位置</summary>
        private Vector2[] scrollPosition;
        /// <summary>人物属性修改界面选选项卡</summary>
        private int showTabDetails = -1;
        /// <summary>
        /// 自定义门派支持度(index:0)/地区恩义(index:1)的锁定值的输入框的缓存值
        /// </summary>
        private string[] tmpCustomLockValue;
        private bool showList;
        private Rect rect;
        private Rect rectList;
        private Rect rectListView;
        private Rect rectListViewGroupTop;
        private Rect rectListViewGroupBottom;
        private readonly int listItemCount = 4;
        private Vector2 scrollPosition2;
        #endregion

        #region Public Class Member
        /// <summary>修改器界面是否已打开</summary>
        public bool Opened { get; private set; }
        #endregion

        /// <summary>
        /// 加载修改器UI
        /// </summary>
        /// <param name="modEntry"></param>
        /// <param name="instance">太吾修改器MOD参数的实例</param>
        /// <returns></returns>
        internal static bool Load(/*Settings instance*/)
        {
            //if (instance == null)
            //{
            //    //Main.logger.Log("[TaiwuEditor] UI.Load() Settings instance is null");
            //    return false;
            //}
            try
            {
                // 创建修改器GUI
                if (Instance == null)
                {
                    //modSettings = instance;
                    new GameObject(typeof(UI).FullName, typeof(UI));
                    windowTexture = new Texture2D(2, 2);
                    if (!windowTexture.LoadImage(Convert.FromBase64String(windowBase64)))
                    {
                        //Main.logger.Log("[TaiwuEditor]UI Background Texture Loading Failure");
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                //Main.logger.Log("[TaiwuEditor]UI Loading Failure");
                //Main.logger.Log(e.ToString());
            }
            return false;
        }

        /// <summary>
        /// UI实例创建时执行
        /// </summary>
        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }

        /// <summary>
        /// 第一次运行实例在Update()前执行一次
        /// </summary>
        private void Start()
        {
            CalculateWindowPos();
            //UpdateTmpValue();
        }

        /// <summary>
        /// 每帧执行一次
        /// </summary>
        private void Update()
        {
            //if (!Main.enabled) return;

            if (Opened)
            {
                //ActorPropertyHelper.Instance.Update(actorId);
            }

            //if (Input.GetKeyUp(modSettings.hotKey))
            //{
            //    ToggleWindow(!Opened);
            //}
        }

        /// <summary>
        /// 渲染GUI
        /// </summary>
        private void OnGUI()
        {
            if (!mInit)
            {
                mInit = true;
                PrepareGUI();
            }
            if (Opened)
            {
                var backgroundColor = GUI.backgroundColor;
                var color = GUI.color;
                GUI.backgroundColor = Color.white;
                GUI.color = Color.white;
                mWindowRect = GUILayout.Window(10086, mWindowRect, WindowFunction, "", windowStyle, GUILayout.Height(Screen.height - 200));
                //DrawList();
                GUI.backgroundColor = backgroundColor;
                GUI.color = color;
            }
        }

        /// <summary>
        /// 计算窗口参数
        /// </summary>
        private void CalculateWindowPos(bool isAdjust = false)
        {
            // 根据DPI调整窗口参数，ratio = 当前窗口宽度/1600，不小于1
            float ratio = Math.Max(Screen.width * 0.000625f, 1f);
            mWindowWidth = windowWidthBase * ratio;
            // 根据屏幕分辨率大小调整窗口位置
            mWindowRect.x = (Screen.width - mWindowWidth) * 0.5f;
            // 根据DPI调整字体大小
            normalFontSize = (int)(normalFontSizeBase * ratio);
            if (!isAdjust)
            {
                mWindowRect.y = 100f;
            }
            else
            {
                // 重置窗口大小
                mWindowRect.width = 0;
                mWindowRect.height = 0;
                // 调整字体
                titleStyle.fontSize = normalFontSize;
                propertiesUIButtonStyle.fontSize = normalFontSize;
                labelStyle.fontSize = normalFontSize;
                nameFieldStyle.fontSize = normalFontSize + 4;
                buttonStyle.fontSize = normalFontSize;
                toggleStyle.fontSize = normalFontSize;
                boxStyle.fontSize = normalFontSize;
                textFieldStyle.fontSize = normalFontSize;
                commentStyle.fontSize = normalFontSize;
            }
            evilButtonWidth = evilButtonWidthBase * ratio;
            healthButtonWidth = healthButtonWidthBase * ratio;
            idFieldWidth = idFieldWidthBase * ratio;
            closeBtnWidth = closeBtnWidthBase * ratio;
            //ActorPropertyHelper.fieldHelperBtnWidth = fieldHelperBtnWidthBase * ratio;
            //ActorPropertyHelper.fieldHelperLblWidth = fieldHelperLblWidthBase * ratio;
            //ActorPropertyHelper.fieldHelperTextWidth = fieldHelperTextWidthBase * ratio;
        }

        /// <summary>
        /// 初始化UI
        /// </summary>
        private void PrepareGUI()
        {
            // 初始化滚动轴
            scrollPosition = new Vector2[funcTab.Length + 1];
            // 初始化UI样式
            windowStyle = new GUIStyle
            {
                name = "te window",
                padding = RectOffset(5)
            };
            windowStyle.normal.background = windowTexture;
            windowStyle.normal.background.wrapMode = TextureWrapMode.Repeat;

            titleStyle = new GUIStyle
            {
                name = "te h1",
                fontSize = normalFontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                margin = RectOffset(0, 5)
            };
            titleStyle.normal.textColor = Color.white;

            propertiesUIButtonStyle = new GUIStyle(GUI.skin.button)
            {
                name = "te h2",
                fontSize = normalFontSize,
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                name = "te normallabel",
                fontSize = normalFontSize,
            };

            nameFieldStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                stretchHeight = true,
                fontSize = normalFontSize + 4
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                name = "te button",
                fontSize = normalFontSize
            };

            toggleStyle = new GUIStyle(GUI.skin.toggle)
            {
                name = "te toggle",
                fontSize = normalFontSize
            };

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                name = "te box",
                fontSize = normalFontSize
            };

            textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                name = "te textfield",
                fontSize = normalFontSize
            };

            commentStyle = new GUIStyle(GUI.skin.label)
            {
                name = "te normallabel",
                fontSize = normalFontSize - 2,
                fontStyle = FontStyle.Bold
            };
            //// 初始化相关样式
            //HelperBase.ButtonStyle = buttonStyle;
            //HelperBase.TextFieldStyle = textFieldStyle;
            //HelperBase.LabelStyle = labelStyle;
            //HelperBase.ToggleStyle = toggleStyle;
            //DropDownMenu.MenuItemStyle = labelStyle;
        }

        /// <summary>
        /// 渲染窗口
        /// </summary>
        /// <param name="windowId">好像没用</param>
        private void WindowFunction(int windowId)
        {
            // 设置标题
            GUILayout.Label($"太吾修改器", titleStyle, GUILayout.Width(mWindowWidth));
            GUILayout.Space(3f);
            SetFuncMenu();
            DrawList();
            GUILayout.FlexibleSpace();
            //GUILayout.Label($"<color=#8FBAE7>[CTRL + F10]</color> 打开 UnityModManager 修改当前快捷键：<color=#F28234>{modSettings.hotKey.ToString()}</color>" +
            //    $"          <color=#8FBAE7>[CTRL + 鼠标左键]</color>   拖动窗口", commentStyle, GUILayout.Width(mWindowWidth));
            GUILayout.Space(5f);
            if (GUILayout.Button("关闭", buttonStyle, GUILayout.Width(closeBtnWidth)))
            {
                ToggleWindow(false);
            }
            if (Input.GetKey(KeyCode.LeftControl))
            {
                GUI.DragWindow();
            }
        }

        /// <summary>
        /// 显示功能选项选择界面
        /// </summary>
        private void SetFuncMenu()
        {
            GUILayout.Label("<color=#87CEEB>功能选择：</color>", labelStyle, GUILayout.Width(mWindowWidth));
            var funcChooseTmp = GUILayout.SelectionGrid(funcChoose, funcTab, funcTab.Length, buttonStyle, GUILayout.Width(mWindowWidth));
            // 每次功能界面切换重置各修改框内的数值为游戏数值
            if (funcChoose != funcChooseTmp && funcChoose == 1)
            {
                //ActorPropertyHelper.Instance.SetAllFieldsNeedToUpdate();
            }
            funcChoose = funcChooseTmp;
            GUILayout.Space(5f);

            switch (funcChoose)
            {
                case 0:
                    //BasicFuncUI();
                    break;
                case 1:
                    BasicPropertiesUI();
                    break;
                case 2:
                    GongFaEditorUI();
                    break;
            }
        }

        ///// <summary>
        ///// 基本功能界面
        ///// </summary>
        //private void BasicFuncUI()
        //{
        //    scrollPosition[0] = GUILayout.BeginScrollView(scrollPosition[0], GUILayout.MinWidth(mWindowWidth));
        //    GUILayout.BeginVertical("Box");
        //    for (int i = 0; i < modSettings.basicUISettings.Length; i++)
        //    {
        //        modSettings.basicUISettings[i] = GUILayout.Toggle(modSettings.basicUISettings[i], modSettings.GetBasicSettingName(i), toggleStyle);
        //        switch (i)
        //        {
        //            case 1:
        //                if (modSettings.basicUISettings[i])
        //                {
        //                    GUILayout.Label($"每次阅读<color=#F28234>{modSettings.pagesPerFastRead}</color>篇(只对功法类书籍有效，技艺类书籍会全部读完)", labelStyle);
        //                    modSettings.pagesPerFastRead = (int)GUILayout.HorizontalScrollbar(modSettings.pagesPerFastRead, 1, 11, 1);
        //                }
        //                break;
        //            case 3:
        //                if (modSettings.basicUISettings[i] && modSettings.includedStoryTyps != null && modSettings.includedStoryTyps.Length == modSettings.includedStoryTyps.Length)
        //                {
        //                    GUILayout.BeginHorizontal("Box");
        //                    if (GUILayout.Button("<color=#F28234>全选</color>", buttonStyle))
        //                    {
        //                        for (int j = 0; j < modSettings.includedStoryTyps.Length; j++)
        //                        {
        //                            modSettings.includedStoryTyps[j] = true;
        //                        }
        //                    }
        //                    for (int j = 0; j < modSettings.includedStoryTyps.Length; j++)
        //                    {
        //                        modSettings.includedStoryTyps[j] = GUILayout.Toggle(modSettings.includedStoryTyps[j], modSettings.GetStoryTyp(j).Name, toggleStyle);
        //                    }
        //                    GUILayout.EndHorizontal();
        //                }
        //                break;
        //            case 8:
        //            case 9:
        //                int choice = i - 8;
        //                if (modSettings.basicUISettings[i] && choice > -1 && choice < modSettings.customLockValue.Length)
        //                {
        //                    string choiceName = modSettings.GetLockValueName(choice);
        //                    GUILayout.Label($"自定义最大{choiceName}(范围0-100)\n<color=#F28234>设置为0则根据剑冢世界进度自动设定最大{choiceName}(推荐)</color>", labelStyle);
        //                    GUILayout.BeginHorizontal();
        //                    tmpCustomLockValue[choice] = GUILayout.TextField(tmpCustomLockValue[choice], textFieldStyle, GUILayout.Width(ActorPropertyHelper.fieldHelperTextWidth));
        //                    if (GUILayout.Button("确定", buttonStyle, GUILayout.Width(ActorPropertyHelper.fieldHelperBtnWidth)))
        //                    {
        //                        if (HelperBase.TryParseInt(tmpCustomLockValue[choice], out int value))
        //                        {
        //                            modSettings.customLockValue[choice] = value;
        //                        }
        //                        else
        //                        {
        //                            tmpCustomLockValue[choice] = modSettings.customLockValue[choice].ToString();
        //                        }
        //                    }
        //                    GUILayout.EndHorizontal();
        //                }
        //                break;
        //        }
        //    }
        //    GUILayout.EndVertical();
        //    GUILayout.EndScrollView();
        //}

        /// <summary>
        /// 设置人物属性修改功能
        /// </summary>
        private void BasicPropertiesUI()
        {
            //DateFile instance = DateFile.instance;
            //if (instance == null || instance.mianActorId == 0)
            //{
            //    GUILayout.Box("未载入存档", boxStyle, GUILayout.Width(mWindowWidth));
            //    return;
            //}
            GUILayout.Label("<color=#87CEEB>修改人物：</color>", labelStyle, GUILayout.Width(mWindowWidth));
            // 选择属性修改的主体
            var basePropertyChooseTmp = GUILayout.SelectionGrid(basePropertyChoose, new string[]
            {
                "<color=#FFA500>太吾本人</color>",
                "<color=#FFA500>最近打开过人物菜单的人物</color>",
                "<color=#FFA500>自定义人物</color>"
            }, 3, buttonStyle, GUILayout.Width(mWindowWidth));
            if (basePropertyChoose != basePropertyChooseTmp)
            {
                //ActorPropertyHelper.Instance.SetAllFieldsNeedToUpdate();
                if (basePropertyChooseTmp == 2)
                {
                    displayedActorId = actorId.ToString();
                }
            }
            basePropertyChoose = basePropertyChooseTmp;
            GUILayout.Label("<color=#87CEEB>修改完成后数值不发生变化是游戏界面没有刷新的原因，并不是修改不成功。" +
                "属性资质修改需重新进入人物菜单才会刷新结果而资源和威望修改需发生对应变化的行为后才会更新。" +
                "所有资质属性均为基础值，不含特性、装备、早熟晚熟以及年龄加成</color>", labelStyle, GUILayout.Width(mWindowWidth));
            // 显示待修改人物的ID
            //DisplayId(instance);
            // 显示待修改人物的姓名
            //GUILayout.Box($"<color=#FFA500>{instance.GetActorName(actorId)}</color>", nameFieldStyle, GUILayout.Width(mWindowWidth));
            var fontStyle = labelStyle.fontStyle;
            labelStyle.fontStyle = FontStyle.Bold;
            GUILayout.Label("<color=#F28234>点击按钮修改对应类型属性</color>", labelStyle, GUILayout.Width(mWindowWidth));
            labelStyle.fontStyle = fontStyle;
            scrollPosition[1] = GUILayout.BeginScrollView(scrollPosition[1], GUILayout.MinWidth(mWindowWidth));
            //ModData(instance);
            GUILayout.EndScrollView();
            /*if (Event.current.type == EventType.Repaint)
            {
                var tmpRect = GUILayoutUtility.GetLastRect();
                Main.logger.Log($"tmprect2: {tmpRect.x} {tmpRect.y} {tmpRect.height} {tmpRect.width}");
            }*/
        }

        private void GongFaEditorUI()
        {
            //DateFile instance = DateFile.instance;
            //if (instance == null || instance.mianActorId == 0)
            //{
            //    GUILayout.Box("未载入存档", boxStyle, GUILayout.Width(mWindowWidth));
            //    return;
            //}
            GUILayout.BeginHorizontal();
            //GongFaHelper.Instance.Editor.GongFaEditBar();
            //GongFaHelper.Instance.Editor.GongFaAddBar();
            GUILayout.EndHorizontal();

            //GongFaHelper.Instance.Editor.GongFaList();

            GUILayout.BeginHorizontal();
            scrollPosition[2] = GUILayout.BeginScrollView(scrollPosition[2], GUILayout.MinWidth(mWindowWidth * 0.5f));
            GUILayout.EndScrollView();
            scrollPosition[3] = GUILayout.BeginScrollView(scrollPosition[3], GUILayout.MinWidth(mWindowWidth * 0.5f));
            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();
        }

        ///// <summary>
        ///// 显示待修改人物的ID
        ///// </summary>
        //private void DisplayId(DateFile instance)
        //{
        //    GUILayout.BeginHorizontal();
        //    GUILayout.Box("<color=#87CEEB>人物ID:</color>", boxStyle, GUILayout.Width(idFieldWidth));
        //    switch (basePropertyChoose)
        //    {
        //        // 修改上次打开人物菜单的人物
        //        case 1:
        //            if (ActorMenu.instance != null && ActorMenu.instance.actorId > -1)
        //            {
        //                actorId = (ActorMenu.instance.actorId == 0) ? instance.mianActorId : ActorMenu.instance.actorId;
        //                GUILayout.Box($"{actorId}", boxStyle, GUILayout.Width(idFieldWidth));
        //            }
        //            break;
        //        case 2:
        //            if (actorId > 0)
        //            {
        //                displayedActorId = GUILayout.TextField(displayedActorId, textFieldStyle, GUILayout.Width(idFieldWidth));
        //                if (GUILayout.Button("确定", buttonStyle, GUILayout.Width(idFieldWidth)))
        //                {
        //                    if (HelperBase.TryParseInt(displayedActorId, out int parsedValue) && parsedValue > -1)
        //                    {
        //                        actorId = (parsedValue == 0) ? instance.mianActorId : parsedValue;
        //                    }
        //                    else
        //                    {
        //                        displayedActorId = actorId.ToString();
        //                    }
        //                }
        //            }
        //            break;
        //        default:
        //            actorId = DateFile.instance.mianActorId;
        //            GUILayout.Box($"{actorId}", boxStyle, GUILayout.Width(idFieldWidth));
        //            break;
        //    }
        //    GUILayout.EndHorizontal();
        //}

        /// <summary>
        /// 修改属性和资源
        /// </summary>
        /// <param name="instance">DateFile类的实例</param>
        private void ModData(/*DateFile instance*/)
        {
            GUILayout.BeginVertical("box");
            for (int k = 0; k < dataTabNames.Length; k++)
            {
                if (GUILayout.Button(dataTabNames[k], propertiesUIButtonStyle, GUILayout.ExpandWidth(true)))
                {
                    //ActorPropertyHelper.Instance.SetAllFieldsNeedToUpdate();
                    showTabDetails = (showTabDetails == k) ? (-1) : k;
                }

                if (showTabDetails == k)
                {
                    switch (k)
                    {
                        case 0:
                            // 基本属性 resid 61-66
                            DisplayDataFields(61, 67);
                            break;
                        case 1:
                            // 资源 resid 401-407
                            DisplayDataFields(401, 408);
                            break;
                        case 2:
                            // 技艺资质 resid 501-516
                            DisplayDataFields(501, 517);
                            break;
                        case 3:
                            // 功法资质 resid 601-614
                            DisplayDataFields(601, 615);
                            break;
                        //case 4:
                        //    if (actorId == instance.mianActorId)
                        //    {
                        //        // 历练 此处resid无实际意义，在update()换算成对应的字段
                        //        ActorPropertyHelper.Instance.FieldHelper(-1);
                        //        // 无属性内力 id 44
                        //        ActorPropertyHelper.Instance.FieldHelper(706);
                        //        GUILayout.Label("每10点无属性内力增加1点真气", labelStyle);
                        //    }
                        //    break;
                        //case 5:
                        //    DisplayHealthAge();
                        //    break;
                        //case 6:
                        //    DisplayXXField(instance);
                        //    break;
                    }
                }
            }
            GUILayout.EndVertical();
            /*if (Event.current.type == EventType.Repaint)
            {
                var tmpRect = GUILayoutUtility.GetLastRect();
                Main.logger.Log($"tmprect1: {tmpRect.x} {tmpRect.y} {tmpRect.height} {tmpRect.width}");
            }*/

        }

        public void DrawList()
        {
            if (showList == true)
            {
                if (listItemCount < dataTabNames.Length)
                {
                    //Main.logger.Log($"rect: {rect.x} {rect.y} {rect.height} {rect.width}");
                    //为了留出最方下的横线,这里高度减1
                    rectList = new Rect(rect.x, rect.y + rect.height, rect.width + 100, rect.height * listItemCount - 1);
                    rectListView = new Rect(rect.x, rect.y + rect.height, rect.width - GUI.skin.verticalScrollbar.fixedWidth, rect.height * dataTabNames.Length);
                    rectListViewGroupTop = new Rect(rectList.x, rectList.y, rectList.width + 100, rectList.height + 1 - rect.height);
                    rectListViewGroupBottom = new Rect(rectList.x, rectList.y + rectListViewGroupTop.height, rectList.width + 100, rect.height);
                    GUI.Box(rectListViewGroupTop, "");
                    GUI.Box(rectListViewGroupBottom, "");
                    //scrollPosition = GUI.BeginScrollView (rectList, scrollPosition, rectListView, false, true);
                    scrollPosition2 = Vector2.Lerp(scrollPosition2, GUI.BeginScrollView(rectList, scrollPosition2, rectListView, false, true), 0.5f);
                    float top = rectList.y;
                    for (int i = 0; i < dataTabNames.Length; i++)
                    {
                        drawItem(new Rect(rectList.x, top, rect.width, rect.height), i);
                        top += rect.height;
                    }
                    GUI.EndScrollView();
                }
                else if (dataTabNames.Length > 0)
                {
                    rectList = new Rect(rect.x, rect.y + rect.height, rect.width, rect.height * dataTabNames.Length - 1);
                    rectListViewGroupTop = new Rect(rectList.x, rectList.y, rectList.width, rectList.height + 1 - rect.height);
                    rectListViewGroupBottom = new Rect(rectList.x, rectList.y + rectListViewGroupTop.height, rectList.width, rect.height);
                    GUI.Box(rectListViewGroupTop, "");
                    GUI.Box(rectListViewGroupBottom, "");
                    GUI.BeginGroup(rectList);
                    float top = 0;
                    for (int i = 0; i < dataTabNames.Length; i++)
                    {
                        drawItem(new Rect(0, top, rect.width, rect.height), i);
                        top += rect.height;
                    }
                    GUI.EndGroup();
                }
            }
        }

        private void drawItem(Rect r, int index)
        {
            if (GUI.Button(r, dataTabNames[index], GUI.skin.label))
            {
                showList = false;
            }
        }

        private void DisplayDataFields(int residBegin, int residEnd)
        {
            if (residBegin >= residEnd)
            {
                return;
            }
            GUILayout.BeginHorizontal();
            // 排版：修改框分三列
            int total = residEnd - residBegin;
            // 除以3的优化(要求count不能超过100)，详见https://www.codeproject.com/KB/cs/FindMulShift.aspx
            int numRow = total * 43 >> 7;
            // 若每列渲染numRow行，则总共剩余rest个Field没有渲染
            int rest = total - numRow * 3;
            // 已渲染行数
            uint rowCount = 0;
            for (int resid = residBegin; resid < residEnd; resid++)
            {
                rowCount++;
                if (rowCount == 1)
                {
                    GUILayout.BeginVertical();
                }
                //ActorPropertyHelper.Instance.FieldHelper(resid);
                // 已渲染行数是否已经符合该列行数要求，注：第一、二列有可能多出一行
                if (rowCount == numRow + ((rest > 0) ? 1 : 0))
                {
                    GUILayout.EndVertical();
                    rest--;
                    rowCount = 0;
                }
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 显示修改健康、寿命选项卡
        /// </summary>
        private void DisplayHealthAge()
        {
            GUILayout.Label("<color=#F28234>注意：\n1.基础寿命为不含人物特性加成的寿命\n2. 人物健康修改为0后，过月就会死亡</color>", labelStyle);
            GUILayout.BeginHorizontal();
            //ActorPropertyHelper.Instance.FieldHelper(11);
            //ActorPropertyHelper.Instance.FieldHelper(13);
            //if (ActorMenu.instance != null)
            //{
            //    ActorPropertyHelper.Instance.FieldHelper(12, DateFile.instance.MaxHealth(actorId));
            //}
            //GUILayout.EndHorizontal();
            //GUILayout.BeginHorizontal();
            //if (GUILayout.Button("一键疗伤", buttonStyle, GUILayout.Width(healthButtonWidth)))
            //{
            //    HelperBase.CureHelper(DateFile.instance, actorId, 0);
            //}
            //if (GUILayout.Button("一键祛毒", buttonStyle, GUILayout.Width(healthButtonWidth)))
            //{
            //    HelperBase.CureHelper(DateFile.instance, actorId, 1);
            //}
            //if (GUILayout.Button("一键调理内息", buttonStyle, GUILayout.Width(healthButtonWidth)))
            //{
            //    HelperBase.CureHelper(DateFile.instance, actorId, 2);
            //}
            //if (GUILayout.Button("我全部都要", buttonStyle, GUILayout.Width(healthButtonWidth)))
            //{
            //    HelperBase.CureHelper(DateFile.instance, actorId, 0);
            //    HelperBase.CureHelper(DateFile.instance, actorId, 1);
            //    HelperBase.CureHelper(DateFile.instance, actorId, 2);
            //}
            GUILayout.EndHorizontal();
        }

        ///// <summary>
        ///// 修改相枢入邪值
        ///// </summary>
        ///// <param name="instance">DateFile类的实例</param>
        //private void DisplayXXField(DateFile instance)
        //{
        //    // 入邪值
        //    int evilValue = HelperBase.LifeDateHelper(instance, actorId, 501);
        //    GUILayout.BeginHorizontal();
        //    GUILayout.Box("当前入邪值: ", boxStyle, GUILayout.Width(evilButtonWidth));
        //    if (evilValue == -1)
        //    {
        //        GUILayout.Box("无", boxStyle, GUILayout.Width(evilButtonWidth));
        //        GUILayout.EndHorizontal();
        //    }
        //    else
        //    {
        //        GUILayout.Box($"{evilValue}", boxStyle, GUILayout.Width(evilButtonWidth));
        //        GUILayout.EndHorizontal();
        //        GUILayout.BeginHorizontal();
        //        if (GUILayout.Button("恢复正常", buttonStyle, GUILayout.Width(evilButtonWidth)))
        //        {
        //            HelperBase.SetActorXXValue(instance, actorId, 0);
        //        }
        //        if (GUILayout.Button("相枢入邪", buttonStyle, GUILayout.Width(evilButtonWidth)))
        //        {
        //            HelperBase.SetActorXXValue(instance, actorId, 100);
        //        }
        //        if (actorId != 0 && actorId != instance.mianActorId)
        //        {
        //            if (GUILayout.Button("相枢化魔", buttonStyle, GUILayout.Width(evilButtonWidth)))
        //            {
        //                HelperBase.SetActorXXValue(instance, actorId, 200);
        //            }
        //        }
        //        GUILayout.EndHorizontal();
        //    }
        //}

        /// <summary>
        /// 打开/关闭修改器窗口
        /// </summary>
        /// <param name="toOpen">True则打开窗口，false关闭窗口</param>
        private void ToggleWindow(bool toOpen)
        {
            //if (!Main.enabled && toOpen)
            //{
            //    return;
            //}
            //Opened = toOpen;
            //BlockGameUI(toOpen);
            //if (!toOpen)
            //{
            //    CalculateWindowPos(true);
            //    UpdateTmpValue();
            //    ActorPropertyHelper.Instance.SetAllFieldsNeedToUpdate();
            //}
        }

        /// <summary>
        /// 屏蔽游戏界面
        /// </summary>
        /// <param name="value"></param>
        private void BlockGameUI(bool value)
        {
            if (value)
            {
                // 屏蔽游戏界面
                mCanvas = new GameObject("TEGameUIBlocker", typeof(Canvas), typeof(GraphicRaycaster));
                mCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                mCanvas.GetComponent<Canvas>().sortingOrder = short.MaxValue;
                DontDestroyOnLoad(mCanvas);
                var panel = new GameObject("TEGameUIBlockerPanel", typeof(Image));
                panel.transform.SetParent(mCanvas.transform);
                panel.GetComponent<RectTransform>().anchorMin = new Vector2(1, 0);
                panel.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
                panel.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                panel.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            }
            else
            {
                Destroy(mCanvas);
            }
        }

        /// <summary>
        /// 更新UI中的缓存值
        /// </summary>
        //private void UpdateTmpValue()
        //{
        //    tmpCustomLockValue = tmpCustomLockValue ?? new string[modSettings.customLockValue.Length];
        //    for (int i = 0; i < tmpCustomLockValue.Length; i++)
        //    {
        //        tmpCustomLockValue[i] = modSettings.customLockValue[i].ToString();
        //    }
        //}

        private static RectOffset RectOffset(int value) => new RectOffset(value, value, value, value);

        private static RectOffset RectOffset(int x, int y) => new RectOffset(x, x, y, y);
    }
}
