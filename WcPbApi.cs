using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{

	public class WcPbApi
	{
		public string[] WcBlockTypeLabels = new string[]
			{
				"Any",
				"Offense",
				"Utility",
				"Power",
				"Production",
				"Thrust",
				"Jumping",
				"Steering"
			};

		private Action<ICollection<MyDefinitionId>> _getCoreWeapons;
		private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, IDictionary<string, int>, bool> _getBlockWeaponMap;
		private Action<IMyTerminalBlock, IDictionary<MyDetectedEntityInfo, float>> _getSortedThreats;
		private Func<long, bool> _hasGridAi;
		//private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, Vector3D?> _getPredictedTargetPos;
		//private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Sandbox.ModAPI.Ingame.MyDetectedEntityInfo> _getWeaponTarget;
		//private Action<ICollection<MyDefinitionId>> _getCoreTurrets;
		private Func<long, int, Sandbox.ModAPI.Ingame.MyDetectedEntityInfo> _getAiFocus;
		private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, bool> _setAiFocus;
		private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool, bool, int> _toggleWeaponFire;
		private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> _hasCoreWeapon;
		private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, ICollection<Sandbox.ModAPI.Ingame.MyDetectedEntityInfo>> _getObstructions;
		private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, ICollection<string>, int, bool> _getTurretTargetTypes;
		private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, ICollection<string>, int> _setTurretTargetTypes;
		private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, Vector3D?> _getPredictedTargetPos;

		private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Matrix> _getWeaponAzimuthMatrix;
		private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Matrix> _getWeaponElevationMatrix;
		private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, MyTuple<bool, Vector3D?>> _isTargetAlignedExtended;
		private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, string> _getActiveAmmo;
		private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, string> _setActiveAmmo;
		private Func<long, float> _getConstructEffectiveDps;
		private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Sandbox.ModAPI.Ingame.MyDetectedEntityInfo> _getWeaponTarget;
		private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int> _setWeaponTarget;

		private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Action<long, int, ulong, long, Vector3D, bool>> _monitorProjectile;
		private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Action<long, int, ulong, long, Vector3D, bool>> _unMonitorProjectile;
		private Func<ulong, MyTuple<Vector3D, Vector3D, float, float, long, string>> _getProjectileState;
		private Func<long, MyTuple<bool, int, int>> _getProjectilesLockedOn;

		private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool, int> _fireWeaponOnce;
		private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, bool, bool, bool> _isWeaponReadyToFire;
		private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, float> _getMaxWeaponRange;
		private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, MyTuple<Vector3D, Vector3D>> _getWeaponScope;
		private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float> _currentPowerConsumption;

		public bool Activate(IMyTerminalBlock pbBlock)
		{
			var dict = pbBlock.GetProperty("WcPbAPI")?.As<IReadOnlyDictionary<string, Delegate>>().GetValue(pbBlock);
			if (dict == null) throw new Exception("WcPbAPI failed to activate");
			return ApiAssign(dict);
		}

		public bool ApiAssign(IReadOnlyDictionary<string, Delegate> delegates)
		{
			if (delegates == null)
				return false;
			AssignMethod(delegates, "GetCoreWeapons", ref _getCoreWeapons);
			AssignMethod(delegates, "GetBlockWeaponMap", ref _getBlockWeaponMap);
			AssignMethod(delegates, "GetSortedThreats", ref _getSortedThreats);
			AssignMethod(delegates, "GetObstructions", ref _getObstructions);
			AssignMethod(delegates, "HasGridAi", ref _hasGridAi);
			AssignMethod(delegates, "GetAiFocus", ref _getAiFocus);
			AssignMethod(delegates, "SetAiFocus", ref _setAiFocus);
			AssignMethod(delegates, "HasCoreWeapon", ref _hasCoreWeapon);
			AssignMethod(delegates, "GetPredictedTargetPosition", ref _getPredictedTargetPos);
			AssignMethod(delegates, "GetTurretTargetTypes", ref _getTurretTargetTypes);
			AssignMethod(delegates, "SetTurretTargetTypes", ref _setTurretTargetTypes);
			AssignMethod(delegates, "GetWeaponAzimuthMatrix", ref _getWeaponAzimuthMatrix);
			AssignMethod(delegates, "GetWeaponElevationMatrix", ref _getWeaponElevationMatrix);
			AssignMethod(delegates, "IsTargetAlignedExtended", ref _isTargetAlignedExtended);
			AssignMethod(delegates, "GetActiveAmmo", ref _getActiveAmmo);
			AssignMethod(delegates, "SetActiveAmmo", ref _setActiveAmmo);
			AssignMethod(delegates, "GetConstructEffectiveDps", ref _getConstructEffectiveDps);
			AssignMethod(delegates, "GetWeaponTarget", ref _getWeaponTarget);
			AssignMethod(delegates, "SetWeaponTarget", ref _setWeaponTarget);
			AssignMethod(delegates, "MonitorProjectile", ref _monitorProjectile);
			AssignMethod(delegates, "UnMonitorProjectile", ref _unMonitorProjectile);
			AssignMethod(delegates, "GetProjectileState", ref _getProjectileState);
			AssignMethod(delegates, "GetProjectilesLockedOn", ref _getProjectilesLockedOn);

			AssignMethod(delegates, "FireWeaponOnce", ref _fireWeaponOnce);
			AssignMethod(delegates, "ToggleWeaponFire", ref _toggleWeaponFire);
			AssignMethod(delegates, "IsWeaponReadyToFire", ref _isWeaponReadyToFire);
			AssignMethod(delegates, "GetMaxWeaponRange", ref _getMaxWeaponRange);
			AssignMethod(delegates, "GetWeaponScope", ref _getWeaponScope);

			AssignMethod(delegates, "GetCurrentPower", ref _currentPowerConsumption);
			return true;
		}
		private void AssignMethod<T>(IReadOnlyDictionary<string, Delegate> delegates, string name, ref T field) where T : class
		{
			if (delegates == null)
			{
				field = null;
				return;
			}
			Delegate del;
			if (!delegates.TryGetValue(name, out del))
				throw new Exception($"{GetType().Name} :: Couldn't find {name} delegate of type {typeof(T)}");
			field = del as T;
			if (field == null)
				throw new Exception(
					$"{GetType().Name} :: Delegate {name} is not type {typeof(T)}, instead it's: {del.GetType()}");
		}

		public void GetAllCoreWeapons(ICollection<MyDefinitionId> collection) => _getCoreWeapons?.Invoke(collection);
		public void GetSortedThreats(IMyTerminalBlock pbBlock, IDictionary<MyDetectedEntityInfo, float> collection) =>
			_getSortedThreats?.Invoke(pbBlock, collection);
		public bool HasGridAi(long entity) => _hasGridAi?.Invoke(entity) ?? false;
		//public Vector3D? GetPredictedTargetPosition(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, long targetEnt, int weaponId) =>_getPredictedTargetPos?.Invoke(weapon, targetEnt, weaponId) ?? null;
		// public MyDetectedEntityInfo? GetWeaponTarget(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId = 0) => _getWeaponTarget?.Invoke(weapon, weaponId);
		//public void GetAllCoreTurrets(ICollection<MyDefinitionId> collection) => _getCoreTurrets?.Invoke(collection);
		public MyDetectedEntityInfo? GetAiFocus(long shooter, int priority = 0) => _getAiFocus?.Invoke(shooter, priority);

		public bool SetAiFocus(Sandbox.ModAPI.Ingame.IMyTerminalBlock pBlock, long target, int priority = 0) =>
			_setAiFocus?.Invoke(pBlock, target, priority) ?? false;

		public void ToggleWeaponFire(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, bool on, bool allWeapons, int weaponId = 0) =>
			_toggleWeaponFire?.Invoke(weapon, on, allWeapons, weaponId);
		public bool HasCoreWeapon(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon) => _hasCoreWeapon?.Invoke(weapon) ?? false;

		public void GetObstructions(Sandbox.ModAPI.Ingame.IMyTerminalBlock pBlock, ICollection<Sandbox.ModAPI.Ingame.MyDetectedEntityInfo> collection) =>
			_getObstructions?.Invoke(pBlock, collection);

		public Vector3D? GetPredictedTargetPosition(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, long targetEnt, int weaponId) =>
			_getPredictedTargetPos?.Invoke(weapon, targetEnt, weaponId) ?? null;

		public Matrix GetWeaponAzimuthMatrix(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId) =>
			_getWeaponAzimuthMatrix?.Invoke(weapon, weaponId) ?? Matrix.Zero;

		public Matrix GetWeaponElevationMatrix(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId) =>
			_getWeaponElevationMatrix?.Invoke(weapon, weaponId) ?? Matrix.Zero;

		public MyTuple<bool, Vector3D?> IsTargetAlignedExtended(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, long targetEnt, int weaponId) =>
			_isTargetAlignedExtended?.Invoke(weapon, targetEnt, weaponId) ?? new MyTuple<bool, Vector3D?>();
		public string GetActiveAmmo(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId) =>
			_getActiveAmmo?.Invoke(weapon, weaponId) ?? null;

		public void SetActiveAmmo(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId, string ammoType) =>
			_setActiveAmmo?.Invoke(weapon, weaponId, ammoType);

		public float GetConstructEffectiveDps(long entity) => _getConstructEffectiveDps?.Invoke(entity) ?? 0f;

		public MyDetectedEntityInfo? GetWeaponTarget(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId = 0) =>
			_getWeaponTarget?.Invoke(weapon, weaponId);

		public void SetWeaponTarget(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, long target, int weaponId = 0) =>
			_setWeaponTarget?.Invoke(weapon, target, weaponId);

		public void MonitorProjectileCallback(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId, Action<long, int, ulong, long, Vector3D, bool> action) =>
			_monitorProjectile?.Invoke(weapon, weaponId, action);

		public void UnMonitorProjectileCallback(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId, Action<long, int, ulong, long, Vector3D, bool> action) =>
			_unMonitorProjectile?.Invoke(weapon, weaponId, action);

		//// POs, Dir, baseDamageLeft, HealthLeft, TargetEntityId, AmmoName
		public MyTuple<Vector3D, Vector3D, float, float, long, string> GetProjectileState(ulong projectileId) =>
			_getProjectileState?.Invoke(projectileId) ?? new MyTuple<Vector3D, Vector3D, float, float, long, string>();

		public bool GetBlockWeaponMap(Sandbox.ModAPI.Ingame.IMyTerminalBlock weaponBlock, IDictionary<string, int> collection) =>
			_getBlockWeaponMap?.Invoke(weaponBlock, collection) ?? false;

		public MyTuple<bool, int, int> GetProjectilesLockedOn(long victim) =>
			_getProjectilesLockedOn?.Invoke(victim) ?? new MyTuple<bool, int, int>();

		public void FireWeaponOnce(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, bool allWeapons = true, int weaponId = 0) =>
				_fireWeaponOnce?.Invoke(weapon, allWeapons, weaponId);


		public bool IsWeaponReadyToFire(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId = 0, bool anyWeaponReady = true,
			bool shootReady = false) =>
			_isWeaponReadyToFire?.Invoke(weapon, weaponId, anyWeaponReady, shootReady) ?? false;

		public float GetMaxWeaponRange(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId) =>
			_getMaxWeaponRange?.Invoke(weapon, weaponId) ?? 0f;

		public MyTuple<Vector3D, Vector3D> GetWeaponScope(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId) =>
			_getWeaponScope?.Invoke(weapon, weaponId) ?? new MyTuple<Vector3D, Vector3D>();
		public float GetCurrentPower(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon) => _currentPowerConsumption?.Invoke(weapon) ?? 0f;
	}


}
