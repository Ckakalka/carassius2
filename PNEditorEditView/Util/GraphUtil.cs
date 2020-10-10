using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PNEditorEditView.Util
{
    public static class GraphUtil
    {
        public static void Prepare(VPetriNet net, List<PetriNetNode> requiredNodes)
        {
            color.Clear();
            processed.Clear();
            GraphUtil.requiredNodes = requiredNodes;
            foreach(PetriNetNode tmp in net.Nodes)
                color.Add(tmp, WHITE);
        }
        // Необходимо вызвать Prepare() перед первым запуском DFS()
        public static void DepthFirstSearchForCycles(PetriNetNode node, ref bool result)
        {
            result = false;
            color[node] = GREY;
            processed.Add(node);
            foreach (VArc arc in node.ThisArcs)
            {
                // если нашли требуемый цикл
                if (result)
                    break;
                // смотрим только на исходящие ребра
                if (arc.From != node)
                    continue;
                if (color[arc.To] == WHITE)
                    DepthFirstSearchForCycles(arc.To, ref result);
                // цикл найден
                if (color[arc.To] == GREY)
                    result = CheckRequiredNodes(arc.To); 
            }
            color[node] = WHITE;
            processed.RemoveAt(processed.Count - 1);
        }
        // проверяет, есть ли в цикле необходимые вершины
        private static bool CheckRequiredNodes(PetriNetNode lastNode)
        {
            int count = 0;
            for(int i = processed.Count - 1; processed[i] != lastNode; --i)
            {
                foreach(PetriNetNode tmp in requiredNodes)
                    if(processed[i] == tmp)
                    {
                        ++count;
                        break;
                    }
                if(count == requiredNodes.Count)
                    return true;
            }
            foreach (PetriNetNode tmp in requiredNodes)
                if (lastNode == tmp)
                {
                    ++count;
                    break;
                }
            return count == requiredNodes.Count;
        }
        private static List<PetriNetNode> requiredNodes;
        // цвета вершин: 0 - белый, 1 - серый
        private static Dictionary<PetriNetNode, int> color = new Dictionary<PetriNetNode, int>();
        private static List<PetriNetNode> processed = new List<PetriNetNode>();
        public const int WHITE = 0;
        public const int GREY  = 1;
    }
}
