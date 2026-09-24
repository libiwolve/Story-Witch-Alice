using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 一键把一个动画倒过来、另存成第二个 clip（例如 AliceTransfer → AliceTransfer_Reverse）。
///
/// 为什么要有这个工具：
///   Unity 里「反向播放一段过渡动画」的标准做法是新建一个 Speed = -1 的状态，
///   但负速度对「非循环 clip」有官方在册的缺陷（会冻住 / 不理会状态转换），
///   而工程里的逐帧 sprite clip 基本都是非循环的。
///   所以改成：把动画本身倒过来存成第二个 clip —— 它就是一个普通的正向动画，
///   没有任何负速度的坑，Exit Time 的语义也和别的状态完全一样。
///
/// 用法：在 Project 窗口里选中一个或多个 .anim，然后点菜单 Tools/Reverse Animation Clip。
/// 生成物一律叫 &lt;原名&gt;_Reverse.anim，放在原文件旁边；已存在会先问你要不要覆盖。
/// 本工具只新增/覆盖这种带 _Reverse 后缀的文件，不碰你任何原始资源。
/// </summary>
public static class ReverseAnimationClipTool
{
    const string Suffix = "_Reverse";
    const string MenuPath = "Tools/Reverse Animation Clip";

    [MenuItem(MenuPath, false, 2000)]
    static void ReverseSelected()
    {
        string reason;
        List<AnimationClip> clips = CollectSelectedClips(out reason);

        if (clips.Count == 0)
        {
            EditorUtility.DisplayDialog("反转动画 Clip",
                string.IsNullOrEmpty(reason) ? "先在 Project 窗口里选中一个或多个 .anim，再点这个菜单。" : reason,
                "好");
            return;
        }

        int ok = 0;
        int failed = 0;
        StringBuilder detail = new StringBuilder();

        foreach (AnimationClip clip in clips)
        {
            string error = ReverseOne(clip);
            if (error == null)
            {
                ok++;
            }
            else
            {
                failed++;
                detail.Append("\n✗ ").Append(clip.name).Append("：").Append(error);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string summary = "成功 " + ok + " 个";
        if (failed > 0) summary += "，失败 " + failed + " 个";
        summary += "。详细结果见 Console（含自检结论）。";

        Debug.Log("[ReverseAnimationClip] " + summary + detail);
        EditorUtility.DisplayDialog("反转动画 Clip", summary + detail, "好");
    }

    [MenuItem(MenuPath, true)]
    static bool ReverseSelectedValidate()
    {
        foreach (UnityEngine.Object o in Selection.objects)
        {
            if (o is AnimationClip) return true;
        }
        return false;
    }

    // ==================== 收集选中项 ====================

    static List<AnimationClip> CollectSelectedClips(out string reason)
    {
        reason = null;
        List<AnimationClip> list = new List<AnimationClip>();

        foreach (UnityEngine.Object o in Selection.objects)
        {
            AnimationClip clip = o as AnimationClip;
            if (clip == null) continue;
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(clip))) continue;   // 内存里临时造的 clip 不处理
            if (clip.name.EndsWith(Suffix)) continue;                              // 别套娃
            if (!list.Contains(clip)) list.Add(clip);
        }

        if (list.Count == 0)
        {
            reason = "选中的东西里没有可处理的 .anim（名字以 " + Suffix + " 结尾的会被跳过，避免套娃）。";
        }
        return list;
    }

    // ==================== 单个 clip ====================

    /// <summary>返回 null 表示成功，否则返回失败原因。</summary>
    static string ReverseOne(AnimationClip src)
    {
        string srcPath = AssetDatabase.GetAssetPath(src);
        string dir = Path.GetDirectoryName(srcPath);
        if (string.IsNullOrEmpty(dir)) return "取不到原文件的目录";
        dir = dir.Replace('\\', '/');

        string dstPath = dir + "/" + src.name + Suffix + ".anim";

        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(dstPath) != null)
        {
            bool overwrite = EditorUtility.DisplayDialog("已经存在同名的反向 Clip", dstPath + "\n\n要覆盖它吗？", "覆盖", "跳过");
            if (!overwrite) return "已经存在，你选择了跳过";
            if (!AssetDatabase.DeleteAsset(dstPath)) return "覆盖失败：删不掉已存在的 " + dstPath;
        }

        // 先整份拷贝：这样循环设置、采样率、事件、时长这些东西全都原样继承，我只需要把曲线倒过来
        if (!AssetDatabase.CopyAsset(srcPath, dstPath)) return "拷贝资源失败（AssetDatabase.CopyAsset 返回 false）";

        AnimationClip dst = AssetDatabase.LoadAssetAtPath<AnimationClip>(dstPath);
        if (dst == null) return "拷贝出来的文件读不出来";

        int spriteKeys = 0;
        int curveKeys = 0;
        int objectCurves = 0;
        int floatCurves = 0;

        // ---- 1) 逐帧换图（sprite / 对象引用）曲线 ----
        foreach (EditorCurveBinding b in AnimationUtility.GetObjectReferenceCurveBindings(src))
        {
            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(src, b);
            if (keys == null || keys.Length == 0) continue;

            ObjectReferenceKeyframe[] mirrored = MirrorObjectKeys(keys);
            AnimationUtility.SetObjectReferenceCurve(dst, b, mirrored);
            spriteKeys += keys.Length;
            objectCurves++;
        }

        // ---- 2) 普通曲线（位移 / 缩放 / 颜色 等）----
        foreach (EditorCurveBinding b in AnimationUtility.GetCurveBindings(src))
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(src, b);
            if (curve == null || curve.length == 0) continue;

            AnimationUtility.SetEditorCurve(dst, b, MirrorCurve(curve));
            curveKeys += curve.length;
            floatCurves++;
        }

        EditorUtility.SetDirty(dst);
        AssetDatabase.SaveAssets();

        // ---- 3) 自检：把生成物读回来，确认真的倒过来了 ----
        string verdict = VerifyReversal(src, dst, out int checkedCurves);

        Debug.Log(string.Format(
            "[ReverseAnimationClip] {0} → {1}\n    对象引用曲线 {2} 条（共 {3} 个 sprite 关键帧），普通曲线 {4} 条（共 {5} 个关键帧）\n    时长 {6:F4}s，采样率 {7}，回读校验 {8} 条曲线\n    自检：{9}",
            src.name, dst.name, objectCurves, spriteKeys, floatCurves, curveKeys, dst.length, dst.frameRate, checkedCurves, verdict));

        return null;
    }

    // ==================== 镜像 ====================

    /// <summary>
    /// 镜像轴取「关键帧时间范围的中点」，所以倒过来之后时间范围（首帧/末帧的时刻）和原来一模一样。
    /// 注意：**不能拿 clip.length 当镜像轴** —— 逐帧 sprite clip 的关键帧常常铺不满时长
    /// （例如关键帧只到 1.0667s、而 clip 时长是 1.0833s），那样倒过来会在开头空出一格。
    /// </summary>
    static ObjectReferenceKeyframe[] MirrorObjectKeys(ObjectReferenceKeyframe[] keys)
    {
        ObjectReferenceKeyframe[] sorted = (ObjectReferenceKeyframe[])keys.Clone();
        System.Array.Sort(sorted, delegate (ObjectReferenceKeyframe a, ObjectReferenceKeyframe b) { return a.time.CompareTo(b.time); });

        float first = sorted[0].time;
        float last = sorted[sorted.Length - 1].time;

        ObjectReferenceKeyframe[] result = new ObjectReferenceKeyframe[sorted.Length];
        for (int i = 0; i < sorted.Length; i++)
        {
            ObjectReferenceKeyframe s = sorted[sorted.Length - 1 - i];
            ObjectReferenceKeyframe k = new ObjectReferenceKeyframe();
            k.time = first + last - s.time;
            k.value = s.value;
            result[i] = k;
        }
        return result;
    }

    static AnimationCurve MirrorCurve(AnimationCurve curve)
    {
        Keyframe[] sorted = curve.keys;     // AnimationCurve 保证按时间升序
        float first = sorted[0].time;
        float last = sorted[sorted.Length - 1].time;

        Keyframe[] result = new Keyframe[sorted.Length];
        for (int i = 0; i < sorted.Length; i++)
        {
            Keyframe s = sorted[sorted.Length - 1 - i];

            Keyframe k = new Keyframe(first + last - s.time, s.value);
            // 时间轴翻向 ⇒ 斜率取反，并且「进」「出」互换
            k.inTangent = -s.outTangent;
            k.outTangent = -s.inTangent;
            k.inWeight = s.outWeight;
            k.outWeight = s.inWeight;
            k.weightedMode = s.weightedMode;

            result[i] = k;
        }
        return new AnimationCurve(result);
    }

    // ==================== 自检 ====================

    /// <summary>
    /// 把生成物读回来对答案：反转后的第一帧必须等于原动画的最后一帧，最后一帧必须等于原动画的第一帧，
    /// 且时间必须落在原来的时间范围内、严格递增。返回一句人话结论。
    /// </summary>
    static string VerifyReversal(AnimationClip src, AnimationClip dst, out int checkedCurves)
    {
        checkedCurves = 0;
        List<string> problems = new List<string>();

        foreach (EditorCurveBinding b in AnimationUtility.GetObjectReferenceCurveBindings(src))
        {
            ObjectReferenceKeyframe[] a = AnimationUtility.GetObjectReferenceCurve(src, b);
            ObjectReferenceKeyframe[] r = AnimationUtility.GetObjectReferenceCurve(dst, b);

            if (a == null || a.Length == 0) continue;

            if (r == null || r.Length != a.Length)
            {
                problems.Add("关键帧数量对不上");
                continue;
            }

            checkedCurves++;

            if (!ReferenceEquals(r[0].value, a[a.Length - 1].value))
                problems.Add("反转后的第 1 帧不是原来的末帧");

            if (!ReferenceEquals(r[r.Length - 1].value, a[0].value))
                problems.Add("反转后的末帧不是原来的第 1 帧");

            for (int i = 1; i < r.Length; i++)
            {
                if (r[i].time <= r[i - 1].time) { problems.Add("时间不是严格递增"); break; }
            }

            if (r[0].time != a[0].time) problems.Add("起点时刻和原来不一致");
            if (r[r.Length - 1].time != a[a.Length - 1].time) problems.Add("终点时刻和原来不一致");
        }

        if (problems.Count == 0)
        {
            return "通过（首末帧已互换，时间轴落在原来的起点与终点之间）";
        }
        return "有问题 → " + string.Join("; ", problems.ToArray());
    }
}
