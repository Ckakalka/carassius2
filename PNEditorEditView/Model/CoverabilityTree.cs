using Core.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.TextFormatting;

namespace PNEditorEditView.Model
{
    class CoverabilityTree
    {
        private CoverabilityTree(Dictionary<PetriNetNode, int> marking, VTransition firedTransition, CoverabilityTree parent)
        {
            this.marking = marking;
            this.firedTransition = firedTransition;
            this.parent = parent;
        }
        public static CoverabilityTree Create(VPetriNet net, ref bool boundness)
        {
            Dictionary<PetriNetNode, int> marking = new Dictionary<PetriNetNode, int>();
            foreach(VPlace place in net.places)
                marking.Add(place, place.NumberOfTokens);
            CoverabilityTree root = new CoverabilityTree(marking, null, null);
            root.constructCoverabilityTree(net, ref boundness);
            return root;
        }

        void constructCoverabilityTree(VPetriNet net, ref bool boundness)
        {
            // проход по всем переходам
            foreach (VTransition tempTransition in net.transitions)
            {
                bool mayBeFired = false;
                // проход по ребрам перехода
                foreach(VArc tempArc in tempTransition.ThisArcs)
                {
                    // смотрим только на входящие в текущий переход
                    if (tempArc.To != tempTransition) continue;

                    mayBeFired = true;
                    int numberOfRequiredTokens;
                    int.TryParse(tempArc.Weight, out numberOfRequiredTokens);
                    int numberOfExistingTokens = marking[tempArc.From];
                    if (numberOfRequiredTokens > numberOfExistingTokens)
                    {
                        mayBeFired = false;
                        break;
                    }
                }
                // если переход может сработать
                if (mayBeFired)
                {
                    Dictionary<PetriNetNode, int> nextMarking = new Dictionary<PetriNetNode, int>(marking);
                    // проход по ребрам перехода
                    foreach (VArc tempArc in tempTransition.ThisArcs)
                    {
                        int weight;
                        int.TryParse(tempArc.Weight, out weight);
                        // если входящее
                        if (tempArc.To == tempTransition)
                        {   // int.MaxValue это "бесконечность"
                            if (nextMarking[tempArc.From] != int.MaxValue)
                                nextMarking[tempArc.From] -= weight;
                        }
                        // если исходящее
                        else if (nextMarking[tempArc.To] != int.MaxValue)
                                nextMarking[tempArc.To] += weight;
                    }
                    CoverabilityTree tmp = this;
                    bool isFoundCover = false;
                    bool isFoundEqual = false;
                    int signСomparisonMarking;
                    while (!(isFoundCover || isFoundEqual || tmp == null))
                    {
                        signСomparisonMarking = compareMarking(nextMarking, tmp.marking, net);
                        if (signСomparisonMarking == 1)
                        {
                            replaceMarking(nextMarking, tmp.marking, net);
                            boundness = false;
                            isFoundCover = true;
                        }
                        if (signСomparisonMarking == 0)
                            isFoundEqual = true;
                        tmp = tmp.parent;
                    }
                    if(children == null)
                        children = new List<CoverabilityTree>();
                    CoverabilityTree child = new CoverabilityTree(nextMarking, tempTransition, this);
                    children.Add(child);
                    foreach(var place in nextMarking)
                        Log += $"value: {place.Value} ";
                    Log += $"{child.firedTransition.Id}\n";
                    if (!isFoundEqual)
                        child.constructCoverabilityTree(net, ref boundness);
                }
            }
        }
        static int compareMarking(Dictionary<PetriNetNode, int> first, Dictionary<PetriNetNode, int> second, VPetriNet net)
        {
            bool isEqual = true;
            foreach (PetriNetNode currentPlace in net.places)
            {
                if (first[currentPlace] >= second[currentPlace])
                {
                    if (isEqual && first[currentPlace] != second[currentPlace])
                        isEqual = false;
                }
                else
                    return -1;
            }
            if (isEqual)
                return 0;
            return 1;
        }
        static void replaceMarking(Dictionary<PetriNetNode, int> first, Dictionary<PetriNetNode, int> second, VPetriNet net)
        {
            foreach (PetriNetNode currentPlace in net.places)
                if (first[currentPlace] > second[currentPlace])
                    first[currentPlace] = int.MaxValue;
        }
        public static string Log = "";
        List<CoverabilityTree> children;
        CoverabilityTree parent;
        VTransition firedTransition;
        Dictionary<PetriNetNode, int> marking;
    }
}
