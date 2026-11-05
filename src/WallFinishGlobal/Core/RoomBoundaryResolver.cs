using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace WallFinishGlobal.Core;

public sealed record RoomWallPair(Room Room, Wall Wall);
public static class RoomBoundaryResolver
{
    public static IReadOnlyList<RoomWallPair> GetPairs(Document doc)
    {
        var options = new SpatialElementBoundaryOptions { SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish };
        var rooms = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Room>().Where(r => r.Area > 0).ToList();
        var pairs = new List<RoomWallPair>(); var seen = new HashSet<(long, long)>();
        foreach (var room in rooms)
        {
            var loops = room.GetBoundarySegments(options);
            if (loops is null) continue;
            foreach (var segment in loops.SelectMany(loop => loop))
                if (doc.GetElement(segment.ElementId) is Wall wall && seen.Add((room.Id.Value, wall.Id.Value))) pairs.Add(new RoomWallPair(room, wall));
        }
        return pairs;
    }
}
