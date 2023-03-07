using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DrakiaXYZ.Waypoints
{
    public class CustomPatrolWay
    {
        public string name;
        public List<CustomWaypoint> waypoints;

        public PatrolType? patrolType;
        public int? maxPersons;
        public WildSpawnType? blockRoles;
    }

    public class CustomWaypoint
    {
        public Vector3 position;
        public bool canUseByBoss;
        public PatrolPointType patrolPointType;
        public bool shallSit;
        public List<CustomWaypoint> waypoints;
    }
}
