using Godot;
using Godot.NativeInterop;

namespace Telepath.Godot;

/// <summary>
/// Hand-written Godot method table for <see cref="View"/>.
/// Telepath.Godot cannot use Godot.SourceGenerators (ScriptPath needs GodotProjectDir;
/// the editor analyzer often has it empty).
/// </summary>
public abstract partial class View
{
#pragma warning disable CS0109
    public new class MethodName : Control.MethodName
    {
        public new static readonly StringName _Ready = "_Ready";
        public new static readonly StringName _EnterTree = "_EnterTree";
        public new static readonly StringName _ExitTree = "_ExitTree";
        public new static readonly StringName _Notification = "_Notification";
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal new static System.Collections.Generic.List<global::Godot.Bridge.MethodInfo> GetGodotMethodList()
    {
        return
        [
            new(name: MethodName._Ready, returnVal: new(type: (Variant.Type)0, name: "", hint: 0, hintString: "", usage: (PropertyUsageFlags)6, exported: false), flags: (MethodFlags)1, arguments: null, defaultArguments: null),
            new(name: MethodName._EnterTree, returnVal: new(type: (Variant.Type)0, name: "", hint: 0, hintString: "", usage: (PropertyUsageFlags)6, exported: false), flags: (MethodFlags)1, arguments: null, defaultArguments: null),
            new(name: MethodName._ExitTree, returnVal: new(type: (Variant.Type)0, name: "", hint: 0, hintString: "", usage: (PropertyUsageFlags)6, exported: false), flags: (MethodFlags)1, arguments: null, defaultArguments: null),
            new(name: MethodName._Notification, returnVal: new(type: (Variant.Type)0, name: "", hint: 0, hintString: "", usage: (PropertyUsageFlags)6, exported: false), flags: (MethodFlags)1, arguments: new() { new(type: (Variant.Type)2, name: "what", hint: 0, hintString: "", usage: (PropertyUsageFlags)6, exported: false) }, defaultArguments: null),
        ];
    }
#pragma warning restore CS0109

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
    {
        if (method == MethodName._Ready && args.Count == 0)
        {
            _Ready();
            ret = default;
            return true;
        }

        if (method == MethodName._EnterTree && args.Count == 0)
        {
            _EnterTree();
            ret = default;
            return true;
        }

        if (method == MethodName._ExitTree && args.Count == 0)
        {
            _ExitTree();
            ret = default;
            return true;
        }

        if (method == MethodName._Notification && args.Count == 1)
        {
            _Notification(VariantUtils.ConvertTo<int>(args[0]));
            ret = default;
            return true;
        }

        return base.InvokeGodotClassMethod(method, args, out ret);
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool HasGodotClassMethod(in godot_string_name method)
    {
        if (method == MethodName._Ready || method == MethodName._EnterTree
            || method == MethodName._ExitTree || method == MethodName._Notification)
        {
            return true;
        }

        return base.HasGodotClassMethod(method);
    }
}
