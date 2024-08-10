using System.Reflection;
using System.Reflection.Emit;

namespace Undefined.Plugins;

public class PluginUpdater
{
    private const string PLUGIN_ENABLE_ACTION_NAME = "OnEnable";
    private const string PLUGIN_DISABLE_ACTION_NAME = "OnDisable";
    private const string PLUGIN_UNLOAD_ACTION_NAME = "OnUnload";
    private const string PLUGIN_IS_ENABLE_FIELD_NAME = "<IsEnabled>k__BackingField";

    private delegate void PluginUpdateAction(PluginBase plugin);

    private readonly PluginUpdateAction[] _actions;

    private readonly FieldInfo _spaceIsEnabledField =
        typeof(PluginBase).GetField(PLUGIN_IS_ENABLE_FIELD_NAME, BindingFlags.Instance | BindingFlags.NonPublic)!;

    public PluginUpdater()
    {
        var values = Enum.GetValues(typeof(PluginAction));
        _actions = new PluginUpdateAction[values.Length];
        _actions[(int)PluginAction.Enable] = CreateEnableDisableAction(PLUGIN_ENABLE_ACTION_NAME);
        _actions[(int)PluginAction.Disable] = CreateEnableDisableAction(PLUGIN_DISABLE_ACTION_NAME);
        _actions[(int)PluginAction.Unload] = CreateUnloadAction();
    }

    public void InvokePluginAction(PluginBase plugin, PluginAction action) => _actions[(int)action](plugin);

    private PluginUpdateAction CreateUnloadAction()
    {
        var method =
            typeof(PluginBase).GetMethod(PLUGIN_UNLOAD_ACTION_NAME, BindingFlags.Instance | BindingFlags.NonPublic)!;
        var dynamicMethod =
            new DynamicMethod("plugins_" + PLUGIN_UNLOAD_ACTION_NAME, null, [typeof(PluginBase)], false);
        var generator = dynamicMethod.GetILGenerator();
        generator.Emit(OpCodes.Nop);
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, method);
        generator.Emit(OpCodes.Ret);
        return (PluginUpdateAction)dynamicMethod.CreateDelegate(typeof(PluginUpdateAction));
    }

    private PluginUpdateAction CreateEnableDisableAction(string name)
    {
        var method = typeof(PluginBase).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
        var dynamicMethod = new DynamicMethod("plugins_" + name, null, [typeof(PluginBase)], false);
        var generator = dynamicMethod.GetILGenerator();
        generator.Emit(OpCodes.Nop);
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, method);
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(name == PLUGIN_ENABLE_ACTION_NAME ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        generator.Emit(OpCodes.Stfld, _spaceIsEnabledField);
        generator.Emit(OpCodes.Ret);
        return (PluginUpdateAction)dynamicMethod.CreateDelegate(typeof(PluginUpdateAction));
    }
}