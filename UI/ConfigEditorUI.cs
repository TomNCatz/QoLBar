﻿using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Dalamud.Interface;
using static QoLBar.BarCfg;
using static QoLBar.ShCfg;

namespace QoLBar
{
    public static class ConfigEditorUI
    {
        public static void EditShortcutConfigBase(ShCfg sh, bool editing)
        {
            if (IconBrowserUI.iconBrowserOpen && IconBrowserUI.doPasteIcon)
            {
                var split = sh.Name.Split(new[] { "##" }, 2, StringSplitOptions.None);
                sh.Name = $"::{IconBrowserUI.pasteIcon}" + (split.Length > 1 ? $"##{split[1]}" : "");
                if (editing)
                    QoLBar.Config.Save();
                IconBrowserUI.doPasteIcon = false;
            }
            if (ImGui.InputText("Name                    ", ref sh.Name, 256) && editing) // Not a bug... just want the window to not change width depending on which type it is...
                QoLBar.Config.Save();
            ImGuiEx.SetItemTooltip("Start the name with ::x where x is a number to use icons, i.e. \"::2914\".\n" +
                "Use ## anywhere in the name to make the text afterwards into a tooltip,\ni.e. \"Name##This is a Tooltip\".");

            var _t = (int)sh.Type;
            ImGui.TextUnformatted("Type");
            ImGui.RadioButton("Command", ref _t, 0);
            ImGui.SameLine(ImGui.GetWindowWidth() / 3);
            ImGui.RadioButton("Category", ref _t, 1);
            ImGui.SameLine(ImGui.GetWindowWidth() / 3 * 2);
            ImGui.RadioButton("Spacer", ref _t, 2);
            if (_t != (int)sh.Type)
            {
                sh.Type = (ShortcutType)_t;
                if (sh.Type == ShortcutType.Category)
                    sh.SubList ??= new List<ShCfg>();

                if (editing)
                    QoLBar.Config.Save();
            }

            if (sh.Type != ShortcutType.Spacer && (sh.Type != ShortcutType.Category || sh.Mode == ShortcutMode.Default))
            {
                var height = ImGui.GetFontSize() * Math.Min(sh.Command.Split('\n').Length + 1, 7) + ImGui.GetStyle().FramePadding.Y * 2; // ImGui issue #238: can't disable multiline scrollbar and it appears a whole line earlier than it should, so thats cool I guess
                if (ImGui.InputTextMultiline("Command##Input", ref sh.Command, 65535, new Vector2(0, height)) && editing)
                    QoLBar.Config.Save();
            }
        }

        public static bool EditShortcutName(ShortcutUI sh)
        {
            if (IconBrowserUI.iconBrowserOpen && IconBrowserUI.doPasteIcon)
            {
                var split = sh.Config.Name.Split(new[] { "##" }, 2, StringSplitOptions.None);
                sh.Config.Name = $"::{IconBrowserUI.pasteIcon}" + (split.Length > 1 ? $"##{split[1]}" : "");
                QoLBar.Config.Save();
                IconBrowserUI.doPasteIcon = false;
            }
            if (ImGui.InputText("Name", ref sh.Config.Name, 256))
            {
                QoLBar.Config.Save();
                return true;
            }
            else
                return false;
        }

        public static bool EditShortcutMode(ShortcutUI sh)
        {
            var _m = (int)sh.Config.Mode;
            ImGui.TextUnformatted("Mode");
            ImGuiEx.SetItemTooltip("Changes the behavior when pressed.\n" +
                "Note: Not intended to be used with categories containing subcategories.");

            ImGui.RadioButton("Default", ref _m, 0);
            ImGuiEx.SetItemTooltip("Default behavior, categories must be set to this to edit their shortcuts!");

            ImGui.SameLine(ImGui.GetWindowWidth() / 3);
            ImGui.RadioButton("Incremental", ref _m, 1);
            ImGuiEx.SetItemTooltip("Executes each line/shortcut in order over multiple presses.");

            ImGui.SameLine(ImGui.GetWindowWidth() / 3 * 2);
            ImGui.RadioButton("Random", ref _m, 2);
            ImGuiEx.SetItemTooltip("Executes a random line/shortcut when pressed.");

            if (_m != (int)sh.Config.Mode)
            {
                sh.Config.Mode = (ShortcutMode)_m;
                QoLBar.Config.Save();

                if (sh.Config.Mode == ShortcutMode.Random)
                {
                    var c = Math.Max(1, (sh.Config.Type == ShortcutType.Category) ? sh.children.Count : sh.Config.Command.Split('\n').Length);
                    sh._i = (int)(QoLBar.GetFrameCount() % c);
                }
                else
                    sh._i = 0;

                return true;
            }
            else
                return false;
        }

        public static bool EditShortcutColor(ShortcutUI sh)
        {
            var color = ImGui.ColorConvertU32ToFloat4(sh.Config.Color);
            color.W += sh.Config.ColorAnimation / 255f; // Temporary
            if (ImGui.ColorEdit4("Color", ref color, ImGuiColorEditFlags.NoDragDrop | ImGuiColorEditFlags.AlphaPreviewHalf))
            {
                sh.Config.Color = ImGui.ColorConvertFloat4ToU32(color);
                sh.Config.ColorAnimation = Math.Max((int)Math.Round(color.W * 255) - 255, 0);
                QoLBar.Config.Save();
                return true;
            }
            else
                return false;
        }

        public static void EditShortcutCategoryOptions(ShortcutUI sh)
        {
            if (ImGui.SliderInt("Button Width", ref sh.Config.CategoryWidth, 0, 200))
                QoLBar.Config.Save();
            ImGuiEx.SetItemTooltip("Set to 0 to use text width.");

            if (ImGui.SliderInt("Columns", ref sh.Config.CategoryColumns, 0, 12))
                QoLBar.Config.Save();
            ImGuiEx.SetItemTooltip("Number of shortcuts in each row before starting another.\n" +
                "Set to 0 to specify infinite.");

            if (ImGui.DragFloat("Scale", ref sh.Config.CategoryScale, 0.002f, 0.7f, 1.5f, "%.2f"))
                QoLBar.Config.Save();

            if (ImGui.DragFloat("Font Scale", ref sh.Config.CategoryFontScale, 0.0018f, 0.5f, 1.0f, "%.2f"))
                QoLBar.Config.Save();

            var spacing = new Vector2(sh.Config.CategorySpacing[0], sh.Config.CategorySpacing[1]);
            if (ImGui.DragFloat2("Spacing", ref spacing, 0.12f, 0, 32, "%.f"))
            {
                sh.Config.CategorySpacing[0] = (int)spacing.X;
                sh.Config.CategorySpacing[1] = (int)spacing.Y;
                QoLBar.Config.Save();
            }

            if (ImGui.Checkbox("Open on Hover", ref sh.Config.CategoryOnHover))
                QoLBar.Config.Save();
            ImGui.SameLine(ImGui.GetWindowWidth() / 2);
            if (ImGui.Checkbox("Stay Open on Selection", ref sh.Config.CategoryStaysOpen))
                QoLBar.Config.Save();
            ImGuiEx.SetItemTooltip("Keeps the category open when pressing shortcuts within it.\nMay not work if the shortcut interacts with other plugins.");

            if (ImGui.Checkbox("No Background", ref sh.Config.CategoryNoBackground))
                QoLBar.Config.Save();
        }

        public static void EditShortcutIconOptions(ShortcutUI sh)
        {
            // Name is available here for ease of access since it pertains to the icon as well
            EditShortcutName(sh);
            ImGuiEx.SetItemTooltip("Icons accept arguments between \"::\" and their ID. I.e. \"::f21\".\n" +
                "\t' f ' - Applies the hotbar frame (or removes it if applied globally).\n" +
                "\t' l ' - Uses the low resolution icon.\n" +
                "\t' h ' - Uses the high resolution icon if it exists.\n" +
                "\t' _ ' - Disables arguments, including implicit ones. Cannot be used with others.");

            if (ImGui.DragFloat("Zoom", ref sh.Config.IconZoom, 0.005f, 1.0f, 5.0f, "%.2f"))
                QoLBar.Config.Save();

            var offset = new Vector2(sh.Config.IconOffset[0], sh.Config.IconOffset[1]);
            if (ImGui.DragFloat2("Offset", ref offset, 0.002f, -0.5f, 0.5f, "%.2f"))
            {
                sh.Config.IconOffset[0] = offset.X;
                sh.Config.IconOffset[1] = offset.Y;
                QoLBar.Config.Save();
            }
        }

        public static void EditBarGeneralOptions(BarUI bar)
        {
            if (ImGui.InputText("Name", ref bar.Config.Name, 256))
                QoLBar.Config.Save();

            var _dock = (int)bar.Config.DockSide;
            if (ImGui.Combo("Side", ref _dock, "Top\0Right\0Bottom\0Left\0Undocked"))
            {
                bar.Config.DockSide = (BarDock)_dock;
                if (bar.Config.DockSide == BarDock.Undocked && bar.Config.Visibility == BarVisibility.Slide)
                    bar.Config.Visibility = BarVisibility.Always;
                bar.Config.Position[0] = 0;
                bar.Config.Position[1] = 0;
                bar.Config.LockedPosition = false;
                QoLBar.Config.Save();
                bar.SetupPivot();
            }

            if (bar.IsDocked)
            {
                var topbottom = bar.Config.DockSide == BarDock.Top || bar.Config.DockSide == BarDock.Bottom;
                var _align = (int)bar.Config.Alignment;
                ImGui.Text("Alignment");
                ImGui.RadioButton(topbottom ? "Left" : "Top", ref _align, 0);
                ImGui.SameLine(ImGui.GetWindowWidth() / 3);
                ImGui.RadioButton("Center", ref _align, 1);
                ImGui.SameLine(ImGui.GetWindowWidth() / 3 * 2);
                ImGui.RadioButton(topbottom ? "Right" : "Bottom", ref _align, 2);
                if (_align != (int)bar.Config.Alignment)
                {
                    bar.Config.Alignment = (BarAlign)_align;
                    QoLBar.Config.Save();
                    bar.SetupPivot();
                }

                var _visibility = (int)bar.Config.Visibility;
                ImGui.Text("Animation");
                ImGui.RadioButton("Slide", ref _visibility, 0);
                ImGui.SameLine(ImGui.GetWindowWidth() / 3);
                ImGui.RadioButton("Immediate", ref _visibility, 1);
                ImGui.SameLine(ImGui.GetWindowWidth() / 3 * 2);
                ImGui.RadioButton("Always Visible", ref _visibility, 2);
                if (_visibility != (int)bar.Config.Visibility)
                {
                    bar.Config.Visibility = (BarVisibility)_visibility;
                    QoLBar.Config.Save();
                }

                if ((bar.Config.Visibility != BarVisibility.Always) && ImGui.DragFloat("Reveal Area Scale", ref bar.Config.RevealAreaScale, 0.01f, 0.0f, 1.0f, "%.2f"))
                    QoLBar.Config.Save();
            }
            else
            {
                var _visibility = (int)bar.Config.Visibility;
                ImGui.Text("Animation");
                ImGui.RadioButton("Immediate", ref _visibility, 1);
                ImGui.SameLine(ImGui.GetWindowWidth() / 2);
                ImGui.RadioButton("Always Visible", ref _visibility, 2);
                if (_visibility != (int)bar.Config.Visibility)
                {
                    bar.Config.Visibility = (BarVisibility)_visibility;
                    QoLBar.Config.Save();
                }
            }

            if (Keybind.KeybindInput(bar.Config))
                bar.tempDisableHotkey = 3; // Takes 2 frames before the game detects this as being held down

            if (ImGui.Checkbox("Edit Mode", ref bar.Config.Editing))
            {
                if (!bar.Config.Editing)
                    QoLBar.Plugin.ExecuteCommand("/echo <se> You can right click on the bar itself (the black background) to reopen this settings menu! You can also use shift + right click to add a new shortcut as well.");
                QoLBar.Config.Save();
            }
            ImGui.SameLine(ImGui.GetWindowWidth() / 2);
            if (ImGui.Checkbox("Lock Position", ref bar.Config.LockedPosition))
                QoLBar.Config.Save();

            if (!bar.Config.LockedPosition)
            {
                var pos = bar.VectorPosition;
                var area = bar.UsableArea;
                var max = (area.X > area.Y) ? area.X : area.Y;
                if (ImGui.DragFloat2(bar.IsDocked ? "Offset" : "Position", ref pos, 1, -max, max, "%.f"))
                {
                    bar.Config.Position[0] = Math.Min(pos.X / area.X, 1);
                    bar.Config.Position[1] = Math.Min(pos.Y / area.Y, 1);
                    QoLBar.Config.Save();
                    if (bar.IsDocked)
                        bar.SetupPivot();
                    else
                        bar._setPos = true;
                }
            }

            if (bar.IsDocked && bar.Config.Visibility != BarVisibility.Always)
            {
                if (ImGui.Checkbox("Hint", ref bar.Config.Hint))
                    QoLBar.Config.Save();
                ImGuiEx.SetItemTooltip("Will prevent the bar from sleeping, increasing CPU load.");
            }
        }

        public static void EditBarStyleOptions(BarUI bar)
        {
            if (ImGui.SliderInt("Button Width", ref bar.Config.ButtonWidth, 0, 200))
                QoLBar.Config.Save();
            ImGuiEx.SetItemTooltip("Set to 0 to use text width.");

            if (ImGui.SliderInt("Columns", ref bar.Config.Columns, 0, 12))
                QoLBar.Config.Save();
            ImGuiEx.SetItemTooltip("Number of shortcuts in each row before starting another.\n" +
                "Set to 0 to specify infinite.");

            if (ImGui.DragFloat("Scale", ref bar.Config.Scale, 0.002f, 0.7f, 2.0f, "%.2f"))
                QoLBar.Config.Save();

            if (ImGui.DragFloat("Font Scale", ref bar.Config.FontScale, 0.0018f, 0.5f, 1.0f, "%.2f"))
                QoLBar.Config.Save();

            var spacing = new Vector2(bar.Config.Spacing[0], bar.Config.Spacing[1]);
            if (ImGui.DragFloat2("Spacing", ref spacing, 0.12f, 0, 32, "%.f"))
            {
                bar.Config.Spacing[0] = (int)spacing.X;
                bar.Config.Spacing[1] = (int)spacing.Y;
                QoLBar.Config.Save();
            }

            if (ImGui.Checkbox("No Background", ref bar.Config.NoBackground))
                QoLBar.Config.Save();
        }
    }
}