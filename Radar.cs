using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
	public partial class Program : MyGridProgram
	{
		List<MyDetectedEntityInfo> WCobstructions = new List<MyDetectedEntityInfo>();
		Dictionary<MyDetectedEntityInfo, float> WCthreats = new Dictionary<MyDetectedEntityInfo, float>();
		MyDetectedEntityInfo focus = new MyDetectedEntityInfo();
		long lfocus = -1;
		int focusChangeTick = -1;

		//char green = (char)253;
		//char red = (char)254;
		//<color={Colour}>

		class DetectedEntity
		{
			public int updTick;

			public long EntityId;
			public string Name = "";
			public MyDetectedEntityType Type;
			public BoundingBoxD BBox;
			public MatrixD Orientation;
			public Vector3D Position;
			public Vector3D Velocity;
			public MyRelationsBetweenPlayerAndBlock Rel = MyRelationsBetweenPlayerAndBlock.Neutral;
			public float threat;

			public MyDetectedEntityInfo focus;
			public double ldistSqr;
			public double distSqr;

			public bool isPMW = false;


			public DetectedEntity upd(MyDetectedEntityInfo e)
			{
				if (e.IsEmpty()) return this;
				updTick = tick;
				EntityId = e.EntityId;
				if (e.Name.Length > 0) Name = e.Name;
				Type = e.Type;
				Orientation = e.Orientation;
				Position = e.Position;
				Velocity = e.Velocity;
				BBox = e.BoundingBox;
				Rel = e.Relationship;
				if ((e.Type == MyDetectedEntityType.CharacterHuman || e.Type == MyDetectedEntityType.CharacterOther) && Name.Length == 0)
				{
					Name = "Suit";// + e.EntityId;
				}
				if (e.Type == MyDetectedEntityType.Unknown)
				{//unknown means obstruction list generally
					if (e.Name.StartsWith("MyVoxelMap"))
					{
						Type = MyDetectedEntityType.Asteroid;
						Name = "Asteroid";
						Rel = MyRelationsBetweenPlayerAndBlock.Neutral;
					}
					else if (e.Name.Length == 0)
					{
						var he = BBox.Max - BBox.Min;
						//grids this small don't actually show up in obstruction list, only suits.
						if (he.X < 3 && he.Y < 3 && he.Z < 3)
						{
							Type = MyDetectedEntityType.CharacterHuman;
							Rel = MyRelationsBetweenPlayerAndBlock.Friends;
							Name = "Suit";
						}
					}
					else Rel = MyRelationsBetweenPlayerAndBlock.Neutral;
				}



				/*{
                    Na*me = ""+e.EntityId;
                }*/
				else if (e.Type == MyDetectedEntityType.Asteroid) Name = "Asteroid";
				else if (e.Type == MyDetectedEntityType.Planet) Name = "Planet";
				if (e.Type == MyDetectedEntityType.LargeGrid) focus = gProgram.modAPIWeaponCore.GetAiFocus(EntityId).GetValueOrDefault();
				return this;
			}
			public DetectedEntity upd(MyDetectedEntityInfo e, float t)
			{
				upd(e);
				threat = t;
				if (e.Name.StartsWith("Small Grid") && Type == MyDetectedEntityType.SmallGrid) isPMW = true;
				if (Type == MyDetectedEntityType.SmallGrid && !isPMW)
				{
					var he = BBox.Max - BBox.Min;
					if (he.X < 10 && he.Y < 10 && he.Z < 10) isPMW = true;
				}

				return this;
			}
		}
		Dictionary<long, DetectedEntity> detectedEntitiesD = new Dictionary<long, DetectedEntity>();
		List<DetectedEntity> detectedEntitiesL = new List<DetectedEntity>();
		void addDE(DetectedEntity e)
		{
			detectedEntitiesD[e.EntityId] = e;
			detectedEntitiesL.Add(e);
		}
		void remDE(DetectedEntity e)
		{
			detectedEntitiesD.Remove(e.EntityId);
			detectedEntitiesL.Remove(e);
		}

		//for skipping fragments
		DetectedEntity findLargestEntity(DetectedEntity s)
		{
			bool hit = false;
			do
			{
				foreach(var e in detectedEntitiesL)
				{
					if(s != e && e.Name.Length < s.Name.Length)
					{
						if(char.IsDigit(s.Name[s.Name.Length-1]) && s.Name.StartsWith(e.Name))
						{
							var distsqr = (e.Position - s.Position).LengthSquared();
							if(distsqr < 100*100)
							{
								s = e;
								hit = true;
								break;
							}
						}
					}
				}
			}while(hit);
			return s;
		}


		int stale_threshold = 20;

		int lastUpdTick = 0;

		SpriteHUDLCD shlcd = null;

		public void updRadar()
		{
			if (tick - lastUpdTick > 60)
			{
				try
				{
					lastUpdTick = tick;
					processThreats();
					if (radarLog != null)
					{
						if (shlcd == null) shlcd = new SpriteHUDLCD(radarLog);
						shlcd.s = radarLog;
						shlcd.setLCD(renderThreats());
					}
				}
				catch (Exception) { }
			}
		}

		public void processThreats()
		{
			if (!modAPIWeaponCoreReady) return;

			long my_id = Me.CubeGrid.EntityId;
			Vector3D my_pos = Me.GetPosition();

			focus = modAPIWeaponCore.GetAiFocus(my_id, 0).GetValueOrDefault();


			if (focus.EntityId != lfocus)
			{
				lfocus = focus.EntityId;
				focusChangeTick = tick;
			}


			// var plo = modAPIWeaponCore.GetProjectilesLockedOn(Me.CubeGrid.EntityId);
			//var plocked = plo.Item2;

			WCobstructions.Clear();
			modAPIWeaponCore.GetObstructions(Me, WCobstructions);
			WCthreats.Clear();
			modAPIWeaponCore.GetSortedThreats(Me, WCthreats);

			foreach (var o in WCobstructions)
			{
				if (!o.IsEmpty())
				{
					DetectedEntity de = null;
					detectedEntitiesD.TryGetValue(o.EntityId, out de);
					if (de != null) de.upd(o);
					else addDE(new DetectedEntity().upd(o));
				}
			}
			foreach (var kvp in WCthreats)
			{
				if (!kvp.Key.IsEmpty())
				{
					DetectedEntity de = null;
					detectedEntitiesD.TryGetValue(kvp.Key.EntityId, out de);
					if (de != null) de.upd(kvp.Key).threat = kvp.Value;
					else
					{
						var n = new DetectedEntity();
						n.upd(kvp.Key).threat = kvp.Value;
						addDE(n);
					}
				}
			}

			List<DetectedEntity> del = new List<DetectedEntity>();
			foreach (var e in detectedEntitiesL)
			{
				if (tick - e.updTick > stale_threshold) del.Add(e);
				else
				{
					e.ldistSqr = e.distSqr;
					e.distSqr = (my_pos - e.Position).LengthSquared();
				}
			}
			foreach (var e in del) remDE(e);


		}

		string getColFromRel(MyRelationsBetweenPlayerAndBlock rel)
		{
			if (rel == MyRelationsBetweenPlayerAndBlock.Enemies) return "<color=red>";
			else if (rel == MyRelationsBetweenPlayerAndBlock.Owner) return "<color=blue>";
			else if (rel == MyRelationsBetweenPlayerAndBlock.Friends || rel == MyRelationsBetweenPlayerAndBlock.FactionShare) return "<color=lightgreen>";
			else if (rel == MyRelationsBetweenPlayerAndBlock.Neutral) return "<color=lightgreen>";
			else return "<color=grey>";
		}
		public string renderThreats()
		{
			StringBuilder b = new StringBuilder();
			//b.Append(" \n");
			detectedEntitiesL.Sort(delegate (DetectedEntity x, DetectedEntity y) {
				double dx = x.distSqr;
				if (x.isPMW) dx += 1000000000;
				double dy = y.distSqr;
				if (y.isPMW) dy += 1000000000;
				return dx.CompareTo(dy);
			});

			long my_id = Me.CubeGrid.EntityId;
			Vector3D my_pos = Me.GetPosition();

			
			var trg = modAPIWeaponCore.GetAiFocus(my_id).GetValueOrDefault();

			if (matchingSpeed)
			{
				b.Append("<color=green>SPEEDMATCHING");
				if (trg.IsEmpty() || trg.Name != matchTarget.Name)
				{
					b.Append(":" + getColFromRel(matchTarget.Rel) + matchTarget.Name);
				}
				else b.Append(" ON");
				b.Append("\n");



				//
				//
				//:" + matchTarget.Name + "\n");
			}
			else b.Append("\n");

			if (!trg.IsEmpty())
			{
				double d = (trg.Position - my_pos).Length();
				b.Append("<color=lightgray>Target: <color=red>" + trg.Name + " (" + dist2str(d) + ")\n");

				IMyTerminalBlock targeter = getSubtargeter();
				if (targeter != null)
				{
					var t = modAPIWeaponCore.GetWeaponTarget(targeter).GetValueOrDefault();
					//Echo("" + (t.Type));
					if (t.Type == MyDetectedEntityType.LargeGrid || t.Type == MyDetectedEntityType.SmallGrid)
					{
						b.Append("<color=lightgray>└Subtarget: <color=red>" + t.Name + "\n");
					}
				}
			}
			else b.Append("<color=lightgray>Target: none\n");
			if (aimbot_txt != "") b.Append("<color=lightgray>" + aimbot_txt);

			int PMWs = 0;
			foreach (var e in detectedEntitiesL)
			{
				if (e.isPMW) PMWs++;
			}
			var plo = modAPIWeaponCore.GetProjectilesLockedOn(my_id);
			var plocked = plo.Item2;
			if (plocked > 0)
			{
				b.Append("<color=red>INBOUND TORPS:" + plocked + "\n");
			}
			if (PMWs > 0)
			{
				b.Append("<color=red>Probable PMWs:" + PMWs + "\n");
			}
			b.Append("\n");

			//b.Append("e: " + detectedEntitiesL.Count + "\n");
			//b.Append("<color=lightgray>e: " + detectedEntitiesL.Count + "\n");
			//b.Append("o: " + WCobstructions.Count + "\n");
			/* foreach (var o in WCobstructions)
             {
                 b.Append(o.Type + "\n");
             }*/



			for (int i = 0; i < detectedEntitiesL.Count; i++)
			{
				var e = detectedEntitiesL[i];
				if (true//e.Rel != MyRelationsBetweenPlayerAndBlock.NoOwnership &&
						//e.Type != MyDetectedEntityType.Unknown &&
						//e.Type != MyDetectedEntityType.Asteroid &&
						//e.Type != MyDetectedEntityType.Planet
					)
				{
					b.Append(getColFromRel(e.Rel));
					// else if (e.Rel == MyRelationsBetweenPlayerAndBlock.NoOwnership) b.Append("<color=lightgray>");

					string n = e.Name;
					//if (e.Name.Length > 30) n = e.Name.Substring(0, 30);
					b.Append(n + " (" + dist2str(Math.Sqrt(e.distSqr)) + ")");// e.Type.ToString()+"\n");
					string thrt;
					if (e.threat < 0.0001) thrt = "0";
					else if (e.threat > 0.1) thrt = e.threat.ToString("0.0");
					else if (e.threat > 0.01) thrt = e.threat.ToString("0.00");
					else thrt = "<0.01";

					/*if (e.threat != 0)*/
					if (e.Rel == MyRelationsBetweenPlayerAndBlock.Enemies) b.Append(" t:" + thrt);
					//if(e.Velocity.LengthSquared() > 1)
					{
						b.Append(" v:" + dist2str(e.Velocity.Length()) + "/s");
					}
					if (!e.focus.IsEmpty())
					{
						b.Append("\n └target:");
						if (e.focus.Relationship == MyRelationsBetweenPlayerAndBlock.Friends) b.Append("<color=lightgreen>");
						else b.Append("<color=lightgray>");
						b.Append(e.focus.Name);
					}


					b.Append("\n");
					/*if(e.Type == MyDetectedEntityType.Unknown)
                    {
                        var he = e.BBox.Max - e.BBox.Min;

                        b.Append(v2ss(he)+"\n");
                    }*/
				}
			}


			return b.ToString();
		}
	}
}
