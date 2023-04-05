import { DependencyContainer } from "tsyringe";
import { IPostDBLoadMod } from "@spt-aki/models/external/IPostDBLoadMod";
import { DatabaseServer } from "@spt-aki/servers/DatabaseServer";

class DrakiaXYZWaypoints implements IPostDBLoadMod {
    public postDBLoad(container: DependencyContainer): void {
        const databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
        const tables = databaseServer.getTables();

        // Make BEAR and USEC move quicker
        for (let diff in tables.bots.types['bear'].difficulty) {
            const diffSetting = tables.bots.types['bear'].difficulty[diff];
            diffSetting.Patrol.LOOK_TIME_BASE = 3;
            diffSetting.Patrol.GO_TO_NEXT_POINT_DELTA = 3;
            diffSetting.Patrol.GO_TO_NEXT_POINT_DELTA_RESERV_WAY = 15;
            diffSetting.Patrol.RESERVE_TIME_STAY = 12;
            diffSetting.Patrol.SPRINT_BETWEEN_CACHED_POINTS = 400;
            diffSetting.Mind.CAN_STAND_BY = false;
        }

        for (let diff in tables.bots.types['usec'].difficulty) {
            const diffSetting = tables.bots.types['usec'].difficulty[diff];
            diffSetting.Patrol.LOOK_TIME_BASE = 3;
            diffSetting.Patrol.GO_TO_NEXT_POINT_DELTA = 3;
            diffSetting.Patrol.GO_TO_NEXT_POINT_DELTA_RESERV_WAY = 15;
            diffSetting.Patrol.RESERVE_TIME_STAY = 12;
            diffSetting.Patrol.SPRINT_BETWEEN_CACHED_POINTS = 400;
            diffSetting.Mind.CAN_STAND_BY = false;
        }

        // Disable path caching for all bot types
        for (let type in tables.bots.types) {
            for (let diff in tables.bots.types[type].difficulty) {
                const diffSetting = tables.bots.types[type].difficulty[diff];
                diffSetting.Patrol.USE_CHACHE_WAYS = false;
            }
        }
    }
}

module.exports = { mod: new DrakiaXYZWaypoints() }