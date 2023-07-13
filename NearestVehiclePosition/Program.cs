class VehiclePosition
{
    public int? VehicleId { get; set; }
    public string? VehicleRegistration { get; set; }
    public float Latitude { get; set; }
    public float Longitude { get; set; }
    public ulong? RecordedTimeUTC { get; set; }
}

class KdTree
{
    private class KdNode
    {
        public VehiclePosition? VehiclePosition { get; set; }
        public KdNode? Left { get; set; }
        public KdNode? Right { get; set; }
    }

    private KdNode? root;

    public void Build(List<VehiclePosition> positions)
    {
        root = BuildRecursive(positions, 0, positions.Count - 1, 0);
    }

    private KdNode? BuildRecursive(List<VehiclePosition> positions, int start, int end, int depth)
    {
        if (start > end)
            return null;

        int axis = depth % 2;
        int mid = (start + end) / 2;

        positions.Sort(start, end - start + 1, Comparer<VehiclePosition>.Create((p1, p2) =>
        {
            return axis == 0 ?
                p1.Latitude.CompareTo(p2.Latitude) :
                p1.Longitude.CompareTo(p2.Longitude);
        }));

        KdNode node = new()
        {
            VehiclePosition = positions[mid],
            Left = BuildRecursive(positions, start, mid - 1, depth + 1),
            Right = BuildRecursive(positions, mid + 1, end, depth + 1)
        };

        return node;
    }

    public VehiclePosition? FindNearestNeighbor(Tuple<float, float> target)
    {
        if (root == null)
            return null;

        KdNode nearestNode = FindNearestNeighborRecursive(root, target, 0);
        return nearestNode.VehiclePosition;
    }

    private KdNode? FindNearestNeighborRecursive(KdNode node, Tuple<float, float> target, int depth)
    {
        if (node == null)
            return null;

        int axis = depth % 2;
        KdNode nextBranch, oppositeBranch;

        if ((axis == 0 && target.Item1 < node.VehiclePosition.Latitude) ||
            (axis == 1 && target.Item2 < node.VehiclePosition.Longitude))
        {
            nextBranch = node.Left;
            oppositeBranch = node.Right;
        }
        else
        {
            nextBranch = node.Right;
            oppositeBranch = node.Left;
        }

        KdNode nearest = FindNearestNeighborRecursive(nextBranch, target, depth + 1);
        if (ShouldReplace(node, nearest, target))
            nearest = node;

        if (oppositeBranch != null && ShouldReplace(node, nearest, target))
        {
            KdNode oppositeNearest = FindNearestNeighborRecursive(oppositeBranch, target, depth + 1);
            if (ShouldReplace(node, nearest, oppositeNearest, target))
                nearest = oppositeNearest;
        }

        return nearest;
    }

    private bool ShouldReplace(KdNode node, KdNode nearest, Tuple<float, float> target)
    {
        if (nearest == null)
            return true;

        double nodeDistance = CalculateDistance(node.VehiclePosition.Latitude, node.VehiclePosition.Longitude, target.Item1, target.Item2);
        double nearestDistance = CalculateDistance(nearest.VehiclePosition.Latitude, nearest.VehiclePosition.Longitude, target.Item1, target.Item2);

        return nodeDistance < nearestDistance;
    }

    private bool ShouldReplace(KdNode node, KdNode nearest, KdNode candidate, Tuple<float, float> target)
    {
        if (candidate == null)
            return false;

        double nodeDistance = CalculateDistance(node.VehiclePosition.Latitude, node.VehiclePosition.Longitude, target.Item1, target.Item2);
        double candidateDistance = CalculateDistance(candidate.VehiclePosition.Latitude, candidate.VehiclePosition.Longitude, target.Item1, target.Item2);
        double nearestDistance = CalculateDistance(nearest.VehiclePosition.Latitude, nearest.VehiclePosition.Longitude, target.Item1, target.Item2);

        return candidateDistance < nearestDistance || (candidateDistance == nearestDistance && nodeDistance < candidateDistance);
    }

    public double CalculateDistance(float lat1, float lon1, float lat2, float lon2)
    {
        double latDiff = lat1 - lat2;
        double lonDiff = lon1 - lon2;
        return Math.Sqrt(latDiff * latDiff + lonDiff * lonDiff);
    }
}

class Program
{
    static List<VehiclePosition> LoadVehiclePositions(string filePath)
    {
        List<VehiclePosition> vehiclePositions = new();

        using (BinaryReader reader = new(File.Open(filePath, FileMode.Open)))
        {
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                VehiclePosition position = new()
                {
                    VehicleId = reader.ReadInt32(),
                    VehicleRegistration = reader.ReadString(),
                    Latitude = reader.ReadSingle(),
                    Longitude = reader.ReadSingle(),
                    RecordedTimeUTC = reader.ReadUInt64()
                };

                vehiclePositions.Add(position);
            }
        }

        return vehiclePositions;
    }

    static void Main()
    {
        string filePath = "D:\\Dev\\NearestVehiclePosition\\NearestVehiclePosition\\data\\VehiclePositions.dat";
        List<Tuple<float, float>> coordinates = new()
        {
            Tuple.Create(34.544909f, -102.100843f),
            Tuple.Create(32.345544f, -99.123124f),
            Tuple.Create(33.234235f, -100.214124f),
            Tuple.Create(35.195739f, -95.348899f),
            Tuple.Create(31.895839f, -97.789573f),
            Tuple.Create(32.895839f, -101.789573f),
            Tuple.Create(34.115839f, -100.225732f),
            Tuple.Create(32.335839f, -99.992232f),
            Tuple.Create(33.535339f, -94.792232f),
            Tuple.Create(32.234235f, -100.222222f)
        };

        // Load the vehicle positions from the binary file
        List<VehiclePosition> vehiclePositions = LoadVehiclePositions(filePath);

        // Build the kd-tree
        KdTree kdTree = new();
        kdTree.Build(vehiclePositions);

        // Find the nearest vehicle position for each given coordinate
        foreach (var coordinate in coordinates)
        {
            VehiclePosition nearestVehicle = kdTree.FindNearestNeighbor(coordinate);
            double distance = kdTree.CalculateDistance(nearestVehicle.Latitude, nearestVehicle.Longitude, coordinate.Item1, coordinate.Item2);
            Console.WriteLine($"Nearest vehicle: {nearestVehicle.VehicleId}, Distance: {distance}");
        }
    }
}
