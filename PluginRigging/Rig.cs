using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BrilliantSkies.Modding;
using BrilliantSkies.Ftd.Constructs.Modules.All.Decorations;
using BrilliantSkies.Ftd.Avatar.Items;
using BrilliantSkies.GridCasts;
using BrilliantSkies.Localisation;
using BrilliantSkies.Localisation.Runtime.FileManagers.Files;
using BrilliantSkies.Core.Types;
using BrilliantSkies.Core.Id;
using UnityEngine;
using BrilliantSkies.Modding.Types;
using BrilliantSkies.Ftd.Avatar.Build.UndoRedo;
using BrilliantSkies.Core.Serialisation.Parameters.Prototypes;
using BrilliantSkies.Ui.Displayer.Types;
using BrilliantSkies.Core.UiSounds;
using BrilliantSkies.Ui.Layouts;
using BrilliantSkies.Ui.Tips;
using BrilliantSkies.Ui.Displayer;
using BrilliantSkies.Ftd.Avatar.Build;
using BrilliantSkies.Ftd.Avatar.Build.Marker;

namespace PluginRigging
{

    public class Rig : CharacterItem
    {
        public new static ILocFile _locFile;

        private Vector3i pos1 = new Vector3i(0, 0, 0);

        private Vector3i pos2 = new Vector3i(0, 0, 0);

        private Decoration dummy;

        private RigGUI _myGui = null;

        public Vector3i mirrorPos = new Vector3i(0, 0, 0);

        public bool mirrorSwitch;

        public float diameter;

        public float distance;

        public Vector2 Off;

        public int color;

        public override string PrimaryFunctionDescription => _locFile.Get("String_PrimaryFunctioN", "Rig");
        public override string SecondaryFunctionDescription => _locFile.Get("String_SecondaryFunction", "Open UI to edit a lot of things");

        static Rig()
        {
            _locFile = Loc.GetFile("Avatar_Items_Rig");
        }


        public Rig()
        {
            diameter = 0.05f;
            Off.x = 0f;
            Off.y = 0f;
            color = 0;
            mirrorPos.x = 0;
            mirrorSwitch = false;
        }

        public override void LeftClick()
        {
            Transform transform = CameraManager.GetSingleton().transform;
            GridCastReturn gridCastReturn = GridCasting.GridCastAllConstructables(new GridCastReturn(transform.position, transform.forward, 300f, 1, countStartingPoint: false)
            {
                ReturnLightBlocks = true
            });

            AllConstruct construct = StaticConstructablesManager.FindClosestConstructable(gridCastReturn.FirstHit.InPointGlobal);
            SafeConstruct C = new SafeConstruct(construct);

            if (!C.TryGet(out var Allconstruct))
            {
                return;
            }

            if (pos1.x == 0 && pos1.y == 0 && pos1.z == 0)
            {
                Vector3 pos = posOnConstruct(gridCastReturn);
                construct.AllBasics.GetClosestBlockToLocalPoint((Vector3i)pos, out pos1);
                dummy = Allconstruct.Decorations.QuickCreate(new ItemDefinition(), pos1, new Quaternion(0,0,0,0), new Vector3(1.5f,1.5f,1.5f), 0, true);
            }
            else
            {
                Vector3 pos = posOnConstruct(gridCastReturn);
                construct.AllBasics.GetClosestBlockToLocalPoint((Vector3i)pos, out pos2);
            }

            if ((pos1.x != 0 && pos2.x != 0) || (pos1.y != 0 && pos2.y != 0) || (pos1.z != 0 && pos2.z != 0))
            {
                AllConstructDecorations decorations = new AllConstructDecorations(construct);

                ItemDefinition item = new ItemDefinition();
                item.ComponentId = new ComponentId(new Guid("ab699540-efc8-4592-bc97-204f6a874b3a"), "unnamed");
                //item.ComponentId = new ComponentId(new Guid("fa52190b-d356-4750-8e60-a8d0400b4831"), "unnamed");
                //item.ComponentId.Guid = new Guid("fa52190b-d356-4750-8e60-a8d0400b4831");
                //item.MaterialReference.Reference = new ComponentId(new Guid("ab699540-efc8-4592-bc97-204f6a874b3a"), "unnamed");
                //MimicHelp.TryGetItemOrObject(new Guid("f5e5a86e-4d3e-433f-8953-d2c9cd8d8366"), out item);
                    
                Vector3i dir = pos1 - pos2;
                Quaternion rot = Quaternion.LookRotation(dir);

                float distance = Vector3.Distance(pos1, pos2);
                Vector3 scale = new Vector3(diameter, diameter, distance);

                Vector3 LocalPos = pos1;

                LocalPos.x -= (float) (0.5) * dir.x + Off.x;
                LocalPos.y -= (float) (0.5) * dir.y + Off.y;
                LocalPos.z -= (float) (0.5) * dir.z;

                Decoration decoration = Allconstruct.Decorations.QuickCreate(item, LocalPos, rot, scale, color, true, pos1);
                //MimicHelp.TryGetItemOrObject(new Guid("fa52190b-d356-4750-8e60-a8d0400b4831"), out var thing);
                //MimicHelp.TryGetMesh(thing, out var result);
                //decoration.ChunkStuff.SetMesh(result.SafeMesh);
                //construct.Chunks.RenderableAdded(decoration);
                //decoration.Changed();

                MirrorInfo mirror = new MirrorInfo(MirrorMode.centre, MirrorPlane.forwardup, mirrorPos);
                if (mirrorSwitch && pos1 != mirror.MirrorPos(pos1))
                {
                    Vector3i mirrorDir = mirror.MirrorPos(pos1) - mirror.MirrorPos(pos2);

                    Vector3 mirrorLocalPos = mirror.MirrorPos(pos1);

                    mirrorLocalPos.x -= (float)(0.5) * mirrorDir.x - Off.x;
                    mirrorLocalPos.y -= (float)(0.5) * mirrorDir.y + Off.y;
                    mirrorLocalPos.z -= (float)(0.5) * mirrorDir.z;

                    Decoration mirrorDecoration = Allconstruct.Decorations.QuickCreate(mirror.plane.MirrorItem(item), mirrorLocalPos, mirror.plane.MirrorRotation(rot), scale, color, false, mirror.MirrorPos(pos1));

                    if (mirrorDecoration == null)
                    {
                        GUISoundManager.GetSingleton().PlayFailure();
                    }
                }


                pos1 = new Vector3i(0, 0, 0);
                pos2 = new Vector3i(0, 0, 0);

                Allconstruct.Decorations.DeleteDecoration(dummy);
                dummy.Delete();

            }
            gridCastReturn.FinishedWithForNow();
        }

        public override void RightClick()
        {
            if (_myGui == null)
            {

                _myGui = new RigGUI();
                _myGui.ActivateGui(this, GuiActivateType.Standard);
            }
            else
            {
                _myGui.DeactivateGui();
                _myGui = null;
            }

        }

        public override bool AreYouTwoHanded()
        {
            return true;
        }

        public void MiddleClick()
        {
            Transform transform = CameraManager.GetSingleton().transform;
            GridCastReturn gridCastReturn = GridCasting.GridCastAllConstructables(new GridCastReturn(transform.position, transform.forward, 300f, 1, countStartingPoint: false)
            {
                ReturnLightBlocks = true
            });

            AllConstruct construct = StaticConstructablesManager.FindClosestConstructable(gridCastReturn.FirstHit.InPointGlobal);
            SafeConstruct C = new SafeConstruct(construct);

            if (!C.TryGet(out var Allconstruct))
            {
                return;
            }

            Vector3 pos = posOnConstruct(gridCastReturn);
            construct.AllBasics.GetClosestBlockToLocalPoint((Vector3i)pos, out mirrorPos);

            GUISoundManager.GetSingleton().PlayBeep();
        }

        Vector3 posOnConstruct(GridCastReturn gridCastReturn)
        {
            if (gridCastReturn.HitSomething)
            {
                GUISoundManager.GetSingleton().PlayBuildSound();

                Block blockHit = gridCastReturn.FirstHit.BlockHit;
                if (blockHit.OnPlayerTeam)
                {

                    Vector3 globalPoint = gridCastReturn.FirstHit.InPointGlobal;
                    IAllConstructBlock constructableOrSubConstructable = blockHit.GetConstructableOrSubConstructable();
                    Vector3 pos = constructableOrSubConstructable.AllBasicsRestricted.GlobalToLocalRounded(globalPoint);

                    return pos;
                }
            }

            return new Vector3(0,0,0);
        }

    }



    public class RigGUI : ThrowAwayObjectGui<Rig>
    {
        public static ILocFile _locFile;

        static RigGUI()
        {
            _locFile = Loc.GetFile("Avatar_Items_RigUi_GUI");
        }

        public override void SetGuiSettings()
        {
            base.GuiSettings.RequiresPlayStop = true;
        }

        public override void OnGui()
        {
            GUILayout.BeginArea(new Rect(50f, 50f, 700f, 300f), _locFile.Get("Area_RiggingSettings", "Rigging Settings"), GUI.skin.window);
            GUISliders.TotalWidthOfWindow = 700;
            GUISliders.TextWidth = 240;
            GUISliders.UpperMargin = 40;
            GUISliders.DecimalPlaces = 0;
            _focus.color = (int)GUISliders.DisplaySlider(0, _locFile.Get("Display_Color", "Color"), _focus.color, 0f, 31f, NoLimitMode.None, new ToolTip(_locFile.Get("Display_Color_Tip", "The Changing of colors")));
            _focus.mirrorPos.x = (int)GUISliders.DisplaySlider(1, _locFile.Get("Display_Mirror", "Mirror x pos"), _focus.mirrorPos.x, -100f, 100f, NoLimitMode.None, new ToolTip(_locFile.Get("Display_Mirror_Tip", "The Changing of mirror X for rope position")));
            GUISliders.DecimalPlaces = 2;
            _focus.diameter = GUISliders.DisplaySlider(2, _locFile.Get("Display_Diameter", "Diameter"), _focus.diameter, 0.05f, 1f, NoLimitMode.None, new ToolTip(_locFile.Get("Display_Diameter_Tip", "The Diameter of the Rope")));
            _focus.Off.x = GUISliders.DisplaySlider(3, _locFile.Get("Display_Off_x", "Off.x"), _focus.Off.x, 0f, 1f, NoLimitMode.None, new ToolTip(_locFile.Get("Display_Off_x_Tip", "The Offset in the x")));
            _focus.Off.y = GUISliders.DisplaySlider(4, _locFile.Get("Display_Off_y", "Off.y"), _focus.Off.y, 0f, 1f, NoLimitMode.None, new ToolTip(_locFile.Get("Display_Off_y_Tip", "The Offset in the y")));


            if (GuiCommon.DisplayCloseButton(700))
            {
                GUISoundManager.GetSingleton().PlayBeep();
                DeactivateGui();
            }
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(50f, 350f, 700f, 400f), _locFile.Get("Area_MirrorSwitch", "ButtonWalhalla"), GUI.skin.window);
            GUISliders.TotalWidthOfWindow = 700;
            GUISliders.TextWidth = 240;
            GUISliders.UpperMargin = 40;
            GuiCommon.DisplayTitleAndIntendedInfo("Switch State of mirror:", "- Mirror is " + Convert.ToString(_focus.mirrorSwitch));
            GuiCommon.Button(_locFile.Get("Display_MirrorSwitch", ""), false, Switch, _locFile.Get("Display_MirrorSwitch_Tip", "Switches the mirror on and off"), GUI.skin.button, GUI.skin.toggle);
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(750f, 50f, 400f, 700f), _locFile.Get("Area_MirrorSwitch", "Information"), GUI.skin.window);
            GuiCommon.DisplayTitleAndIntendedInfo("Description of Rigging mod", "This mod is made by Jeoshy to make rigging life easier.");
            GuiCommon.DisplayTitleAndIntendedInfo("- Left Click", "Press Left twice to place rope");
            GuiCommon.DisplayTitleAndIntendedInfo("- Right Click", "Congratulations! You found this menu");
            //GuiCommon.DisplayTitleAndIntendedInfo("Middle Click", "Changes the mirror X position");
            GuiCommon.DisplayTitleAndIntendedInfo("The rest", "is pretty self-explanatory :)");
            GUILayout.EndArea();
        }

        public void Switch()
        {
            GUISoundManager.GetSingleton().PlayBeep();

            if (_focus.mirrorSwitch)
            {
                _focus.mirrorSwitch = false;
            }
            else
            {
                _focus.mirrorSwitch = true;

            }
        }

    }





}