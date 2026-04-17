using System;
using System.Collections.Generic;

[Serializable]
public class OntologyData
{
    public MapOntology map;
    public List<KnowledgeNode> crops;
    public List<KnowledgeNode> system_rules;
}

[Serializable]
public class KnowledgeNode
{
    public string name;
    public List<string> keywords;
    public string info;
}

[Serializable]
public class MapOntology
{
    public string name;
    public MapSize size;
    public CoordinateSystem coordinate_system;
}

[Serializable]
public class MapSize
{
    public int width;
    public int height;
    public string unit;
}

[Serializable]
public class CoordinateSystem
{
    public Origin origin;
    public string x_direction;
    public string y_direction;
    public CornerSet corners;
    public ValidRange valid_range;
    public string info;
}

[Serializable]
public class Origin
{
    public int x;
    public int y;
    public string description;
}

[Serializable]
public class CornerSet
{
    public int[] top_left;
    public int[] top_right;
    public int[] bottom_left;
    public int[] bottom_right;
}

[Serializable]
public class ValidRange
{
    public int[] x;
    public int[] y;
}
