using System.Collections.Generic;

public class SynthesisNode
{
    public ElementData elementData;
    public List<SynthesisNode> children;
    public SynthesisNode(ElementData elementData,List<SynthesisNode> children=null)
    {
        this.elementData = elementData;
        this.children = children ?? new List<SynthesisNode>();
    }
}
