//------------------------------------------------------------------------------
// <copyright file="ContentValidator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Diagnostics;

namespace System.Xml.Schema {

    #region ExceptionSymbolsPositions
    
    /// <summary>
    /// UPA violations will throw this exception
    /// </summary>
    class UpaException : Exception {
        object particle1;
        object particle2;
        public UpaException(object particle1, object particle2) {
            this.particle1 = particle1;
            this.particle2 = particle2;
        }
        public object Particle1 { get { return particle1; } }
        public object Particle2 { get { return particle2; } }
    }

    /// <summary>
    /// SymbolsDictionary is a map between names that ContextValidator recognizes and symbols - int symbol[XmlQualifiedName name].
    /// There are two types of name - full names and wildcards (namespace is specified, local name is anythig).
    /// Wildcard excludes all full names that would match by the namespace part.
    /// SymbolsDictionry alwas recognizes all the symbols - the last one is a true wildcard - 
    ///      both name and namespace can be anything that none of the other symbols matched.
    /// </summary>
    class SymbolsDictionary {
        int last = 0;
        Hashtable names;
        Hashtable wildcards = null;
        ArrayList particles;
        object particleLast = null; 
        bool isUpaEnforced = true;

        public SymbolsDictionary() {
            names = new Hashtable();
            particles = new ArrayList();
        }

        public int Count {
            // last one is a "*:*" any wildcard
            get { return last + 1; }
        }
        
        public int CountOfNames {
            get { return names.Count; }
        }

        /// <summary>
        /// True is particle can be deterministically attributed from the symbol and conversion to DFA is possible.
        /// </summary>
        public bool IsUpaEnforced {
            get { return isUpaEnforced; }
            set { isUpaEnforced = value; }
        }
        
        /// <summary>
        /// Add name  and return it's number
        /// </summary>
        public int AddName(XmlQualifiedName name, object particle) {
            object lookup = names[name];
            if (lookup != null) {
                int symbol = (int)lookup;
                if (particles[symbol] != particle) {
                    isUpaEnforced = false;
                }
                return symbol;
            }
            else {
                names.Add(name, last);
                particles.Add(particle);
                Debug.Assert(particles.Count == last + 1);
                return last ++;
            }
        }
        
        public void AddNamespaceList(NamespaceList list, object particle, bool allowLocal) {
            switch (list.Type) {
            case NamespaceList.ListType.Any:
                particleLast = particle;   
                break;
            case NamespaceList.ListType.Other:
                // Create a symbol for the excluded namespace, but don't set a particle for it.
                AddWildcard(list.Excluded, null);
                if (!allowLocal) {
                    AddWildcard(string.Empty, null); //##local is not allowed
                }
                break; 
            case NamespaceList.ListType.Set:
                foreach(string wildcard in list.Enumerate) {
                    AddWildcard(wildcard, particle);
                }
                break;
            }
        }

        private void AddWildcard(string wildcard, object particle) {
            if (wildcards == null) {
                wildcards = new Hashtable();
            }
            object lookup = wildcards[wildcard];
            if (lookup == null) {
                wildcards.Add(wildcard, last);
                particles.Add(particle);
                Debug.Assert(particles.Count == last + 1);
                last ++;
            }
            else if (particle != null) {
                particles[(int)lookup] = particle;    
            }
        }

        public ICollection GetNamespaceListSymbols(NamespaceList list) {
            ArrayList match = new ArrayList();
            foreach(XmlQualifiedName name in names.Keys) {
                if (name != XmlQualifiedName.Empty && list.Allows(name)) {
                    match.Add(names[name]);
                }
            }
            if (wildcards != null) {
                foreach(string wildcard in wildcards.Keys) {
                    if (list.Allows(wildcard)) {
                        match.Add(wildcards[wildcard]);
                    }
                }              
            }
            if (list.Type == NamespaceList.ListType.Any || list.Type == NamespaceList.ListType.Other) {
                match.Add(last); // add wildcard
            }
            return match;
        }

        /// <summary>
        /// Find the symbol for the given name. If neither names nor wilcards match it last (*.*) symbol will be returned
        /// </summary>
        public int this[XmlQualifiedName name] {
            get {
                object lookup = names[name];
                if (lookup != null) {
                    return (int)lookup;
                }
                if (wildcards != null) {
                    lookup = wildcards[name.Namespace];
                    if (lookup != null) {
                        return (int)lookup;
                    }   
                }
                return last; // true wildcard
            }
        }
        
        /// <summary>
        /// Check if a name exists in the symbol dictionary
        /// </summary>
        public bool Exists(XmlQualifiedName name) {

            object lookup = names[name];
            if (lookup != null) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Return content processing mode for the symbol
        /// </summary>
        public object GetParticle(int symbol) {
            return symbol == last ? particleLast : particles[symbol];
        }

        /// <summary>
        /// Output symbol's name
        /// </summary>
        public string NameOf(int symbol) {
            foreach (DictionaryEntry de in names) {
                if ((int)de.Value == symbol) {
                    return ((XmlQualifiedName)de.Key).ToString();
                }
            }
            if (wildcards != null) {
                foreach (DictionaryEntry de in wildcards) {
                    if ((int)de.Value == symbol) {
                        return (string)de.Key + ":*";
                    }
                }
            }
            return "##other:*";
        }
    }

    struct Position {
        public int symbol;
        public object particle;
        public Position(int symbol, object particle) {
            this.symbol = symbol;
            this.particle = particle;
        }
    }

    class Positions {
        ArrayList positions = new ArrayList();

        public int Add(int symbol, object particle) {
            return positions.Add(new Position(symbol, particle));
        }

        public Position this[int pos] {
            get { return (Position)positions[pos]; }
        }

        public int Count {
            get { return positions.Count; }
        }
    }
    #endregion

    #region SystaxTree
    /// <summary>
    /// Base class for the systax tree nodes
    /// </summary>
    abstract class SyntaxTreeNode {
        /// <summary>
        /// Expand NamesapceListNode and RangeNode nodes. All other nodes
        /// </summary>
        public abstract void ExpandTree(InteriorNode parent, SymbolsDictionary symbols, Positions positions);

        /// <summary>
        /// Clone the syntaxTree. We need to pass symbolsByPosition because leaf nodes have to add themselves to it.
        /// </summary>
        public abstract SyntaxTreeNode Clone(Positions positions);

        /// <summary>
        /// From a regular expression to a DFA
        /// Compilers by Aho, Sethi, Ullman.
        /// ISBN 0-201-10088-6, p135 
        /// Construct firstpos, lastpos and calculate followpos 
        /// </summary>
        public abstract void ConstructPos(BitSet firstpos, BitSet lastpos, BitSet[] followpos);

        /// <summary>
        /// Returns nullable property that is being used by ConstructPos
        /// </summary>
        public abstract bool IsNullable { get; }

        /// <summary>
        /// Returns true if node is a range node
        /// </summary>
        public virtual bool IsRangeNode { 
            get {
                return false;
            }
        }

        /// <summary>
        /// Print syntax tree
        /// </summary>
#if DEBUG        
        public abstract void Dump(StringBuilder bb, SymbolsDictionary symbols, Positions positions);
#endif
    }

    /// <summary>
    /// Terminal of the syntax tree
    /// </summary>
    class LeafNode : SyntaxTreeNode {
        int pos;

        public LeafNode(int pos) {
            this.pos = pos;
        }

        public int Pos {
            get { return pos;}
            set { pos = value; }
        }

        public override void ExpandTree(InteriorNode parent, SymbolsDictionary symbols, Positions positions) {
            // do nothing
        }

        public override SyntaxTreeNode Clone(Positions positions) {
            return new LeafNode(positions.Add(positions[pos].symbol, positions[pos].particle));
        }

        public override void ConstructPos(BitSet firstpos, BitSet lastpos, BitSet[] followpos) {
            firstpos.Set(pos);
            lastpos.Set(pos);
        }

        public override bool IsNullable {
            get { return false; }
        }

#if DEBUG
        public override void Dump(StringBuilder bb, SymbolsDictionary symbols, Positions positions) {
            bb.Append("\"" + symbols.NameOf(positions[pos].symbol) + "\"");
        }
#endif
    }

    /// <summary>
    /// Temporary node to represent NamespaceList. Will be expended as a choice of symbols
    /// </summary>
    class NamespaceListNode : SyntaxTreeNode {
        protected NamespaceList namespaceList;
        protected object particle;

        public NamespaceListNode(NamespaceList namespaceList, object particle) {
            this.namespaceList = namespaceList;
            this.particle = particle;
        }

        public override SyntaxTreeNode Clone(Positions positions) {
            // NamespaceListNode nodes have to be removed prior to that
            throw new InvalidOperationException();
        }

        public virtual ICollection GetResolvedSymbols(SymbolsDictionary symbols)  {
            return symbols.GetNamespaceListSymbols(namespaceList);
        }

        public override void ExpandTree(InteriorNode parent, SymbolsDictionary symbols, Positions positions) {
            SyntaxTreeNode replacementNode = null;
            foreach(int symbol in GetResolvedSymbols(symbols)) {
                if (symbols.GetParticle(symbol) != particle) {
                    symbols.IsUpaEnforced = false;
                }
                LeafNode node = new LeafNode(positions.Add(symbol, particle));
                if (replacementNode == null) {
                    replacementNode = node;
                }
                else {
                    InteriorNode choice = new ChoiceNode();
                    choice.LeftChild = replacementNode;
                    choice.RightChild = node;
                    replacementNode = choice;
                }
            }
            if (parent.LeftChild == this) {
                parent.LeftChild = replacementNode;
            }
            else {
                parent.RightChild = replacementNode;
            }
        }

        public override void ConstructPos(BitSet firstpos, BitSet lastpos, BitSet[] followpos) {
            // NamespaceListNode nodes have to be removed prior to that
            throw new InvalidOperationException();
        }

        public override bool IsNullable {
            // NamespaceListNode nodes have to be removed prior to that
            get { throw new InvalidOperationException(); }
        }

#if DEBUG
        public override void Dump(StringBuilder bb, SymbolsDictionary symbols, Positions positions) {
            bb.Append("[" + namespaceList.ToString() + "]");
        }
#endif
    }

    /// <summary>
    /// Base class for all internal node. Note that only sequence and choice have right child
    /// </summary>
    abstract class InteriorNode : SyntaxTreeNode {
        SyntaxTreeNode leftChild;
        SyntaxTreeNode rightChild;

        public SyntaxTreeNode LeftChild {
            get { return leftChild;}
            set { leftChild = value;}
        }

        public SyntaxTreeNode RightChild {
            get { return rightChild;}
            set { rightChild = value;}
        }

        public override SyntaxTreeNode Clone(Positions positions) {
            InteriorNode other = (InteriorNode)this.MemberwiseClone();
            other.LeftChild = leftChild.Clone(positions);
            if (rightChild != null) {
                other.RightChild = rightChild.Clone(positions);
            }
            return other;
        }

        //no recursive version of expand tree for Sequence and Choice node
        protected void ExpandTreeNoRecursive(InteriorNode parent, SymbolsDictionary symbols, Positions positions) {
            Stack<InteriorNode> nodeStack = new Stack<InteriorNode>();
            InteriorNode this_ = this;
            while (true) {
                if (this_.leftChild is ChoiceNode || this_.leftChild is SequenceNode) {
                    nodeStack.Push(this_);
                    this_ = (InteriorNode)this_.leftChild;
                    continue;
                }
                this_.leftChild.ExpandTree(this_, symbols, positions);

            ProcessRight:
                if (this_.rightChild != null) {
                    this_.rightChild.ExpandTree(this_, symbols, positions);
                }

                if (nodeStack.Count == 0)
                    break;

                this_ = nodeStack.Pop();
                goto ProcessRight;
            }
        }

        public override void ExpandTree(InteriorNode parent, SymbolsDictionary symbols, Positions positions) {
            leftChild.ExpandTree(this, symbols, positions);
            if (rightChild != null) {
                rightChild.ExpandTree(this, symbols, positions);
            }
        }
    }

    
    sealed class SequenceNode : InteriorNode {

        struct SequenceConstructPosContext {
            public SequenceNode this_;
            public BitSet firstpos;
            public BitSet lastpos;
            public BitSet lastposLeft;
            public BitSet firstposRight;

            public SequenceConstructPosContext(SequenceNode node, BitSet firstpos, BitSet lastpos) {
                this_ = node;
                this.firstpos = firstpos;
                this.lastpos = lastpos;

                lastposLeft = null;
                firstposRight = null;
            }
        }

        public override void ConstructPos(BitSet firstpos, BitSet lastpos, BitSet[] followpos) {

            Stack<SequenceConstructPosContext> contextStack = new Stack<SequenceConstructPosContext>();
            SequenceConstructPosContext context = new SequenceConstructPosContext(this, firstpos, lastpos);

            while (true) {
                SequenceNode this_ = context.this_;
                context.lastposLeft = new BitSet(lastpos.Count);
                if (this_.LeftChild is SequenceNode) {
                    contextStack.Push(context);
                    context = new SequenceConstructPosContext((SequenceNode)this_.LeftChild, context.firstpos, context.lastposLeft);
                    continue;
                }
                this_.LeftChild.ConstructPos(context.firstpos, context.lastposLeft, followpos);

            ProcessRight:
                context.firstposRight = new BitSet(firstpos.Count);
                this_.RightChild.ConstructPos(context.firstposRight, context.lastpos, followpos);

                if (this_.LeftChild.IsNullable && !this_.RightChild.IsRangeNode) {
                    context.firstpos.Or(context.firstposRight);
                }
                if (this_.RightChild.IsNullable) {
                    context.lastpos.Or(context.lastposLeft);
                }
                for (int pos = context.lastposLeft.NextSet(-1); pos != -1; pos = context.lastposLeft.NextSet(pos)) {
                    followpos[pos].Or(context.firstposRight);
                }
                if (this_.RightChild.IsRangeNode) { //firstpos is leftchild.firstpos as the or with firstposRight has not been done as it is a rangenode
                    ((LeafRangeNode)this_.RightChild).NextIteration = context.firstpos.Clone();
                }

                if (contextStack.Count == 0)
                    break;

                context = contextStack.Pop();
                this_ = context.this_;
                goto ProcessRight;
            }
        }

        public override bool IsNullable {
            get {
                SyntaxTreeNode n;
                SequenceNode this_ = this;
                do {
                    if (this_.RightChild.IsRangeNode && ((LeafRangeNode)this_.RightChild).Min == 0)
                        return true;
                    if (!this_.RightChild.IsNullable && !this_.RightChild.IsRangeNode)
                        return false;
                    n = this_.LeftChild;
                    this_ = n as SequenceNode;
                }
                while (this_ != null);
                return n.IsNullable;
            }
        }

        public override void ExpandTree(InteriorNode parent, SymbolsDictionary symbols, Positions positions) {
 	        ExpandTreeNoRecursive(parent, symbols, positions);
        }

#if DEBUG
        internal static void WritePos(BitSet firstpos, BitSet lastpos, BitSet[] followpos) {
            Debug.WriteLineIf(DiagnosticsSwitches.XmlSchemaContentModel.Enabled, "FirstPos:  ");
            WriteBitSet(firstpos);

            Debug.WriteLineIf(DiagnosticsSwitches.XmlSchemaContentModel.Enabled, "LastPos:  ");
            WriteBitSet(lastpos);

            Debug.WriteLineIf(DiagnosticsSwitches.XmlSchemaContentModel.Enabled, "Followpos:  ");
            for(int i =0; i < followpos.Length; i++) {
                WriteBitSet(followpos[i]);
            }
        }
        internal static void WriteBitSet(BitSet curpos) {
            int[] list = new int[curpos.Count];
            for (int pos = curpos.NextSet(-1); pos != -1; pos = curpos.NextSet(pos)) {
                list[pos] = 1;
            }
            for(int i = 0; i < list.Length; i++) {
                Debug.WriteIf(DiagnosticsSwitches.XmlSchemaContentModel.Enabled, list[i] + " ");
            }
            Debug.WriteLineIf(DiagnosticsSwitches.XmlSchemaContentModel.Enabled, "");
        }
       

        public override void Dump(StringBuilder bb, SymbolsDictionary symbols, Positions positions) {
            Stack<SequenceNode> nodeStack = new Stack<SequenceNode>();
            SequenceNode this_ = this;

            while (true) {
                bb.Append("(");
                if (this_.LeftChild is SequenceNode) {
                    nodeStack.Push(this_);
                    this_ = (SequenceNode)this_.LeftChild;
                    continue;
                }
                this_.LeftChild.Dump(bb, symbols, positions);

            ProcessRight:
                bb.Append(", ");
                this_.RightChild.Dump(bb, symbols, positions);
                bb.Append(")");
                if (nodeStack.Count == 0)
                    break;

                this_ = nodeStack.Pop();
                goto ProcessRight;
            }
        }
#endif

    }

    sealed class ChoiceNode : InteriorNode {

        private static void ConstructChildPos(SyntaxTreeNode child, BitSet firstpos, BitSet lastpos, BitSet[] followpos) {
            BitSet firstPosTemp = new BitSet(firstpos.Count);
            BitSet lastPosTemp = new BitSet(lastpos.Count);
            child.ConstructPos(firstPosTemp, lastPosTemp, followpos);
            firstpos.Or(firstPosTemp);
            lastpos.Or(lastPosTemp);
        }

        public override void ConstructPos(BitSet firstpos, BitSet lastpos, BitSet[] followpos) {

            BitSet firstPosTemp = new BitSet(firstpos.Count);
            BitSet lastPosTemp = new BitSet(lastpos.Count);
            SyntaxTreeNode n;
            ChoiceNode this_ = this;
            do {
                ConstructChildPos(this_.RightChild, firstPosTemp, lastPosTemp, followpos);
                n = this_.LeftChild;
                this_ = n as ChoiceNode;
            } while (this_ != null);

            n.ConstructPos(firstpos, lastpos, followpos);
            firstpos.Or(firstPosTemp);
            lastpos.Or(lastPosTemp);
        }

        public override bool IsNullable {
            get {
                SyntaxTreeNode n;
                ChoiceNode this_ = this;
                do {
                    if (this_.RightChild.IsNullable)
                        return true;
                    n = this_.LeftChild;
                    this_ = n as ChoiceNode;
                }
                while (this_ != null);
                return n.IsNullable;
            }
        }

        public override void ExpandTree(InteriorNode parent, SymbolsDictionary symbols, Positions positions) {
 	        ExpandTreeNoRecursive(parent, symbols, positions);
        }

#if DEBUG
        public override void Dump(StringBuilder bb, SymbolsDictionary symbols, Positions positions) {
            Stack<ChoiceNode> nodeStack = new Stack<ChoiceNode>();
            ChoiceNode this_ = this;

            while (true) {
                bb.Append("(");
                if (this_.LeftChild is ChoiceNode) {
                    nodeStack.Push(this_);
                    this_ = (ChoiceNode)this_.LeftChild;
                    continue;
                }
                this_.LeftChild.Dump(bb, symbols, positions);

            ProcessRight:
                bb.Append(" | ");
                this_.RightChild.Dump(bb, symbols, positions);
                bb.Append(")");
                if (nodeStack.Count == 0)
                    break;

                this_ = nodeStack.Pop();
                goto ProcessRight;
            }
        }
#endif
    }
    
    sealed class PlusNode : InteriorNode {
        public override void ConstructPos(BitSet firstpos, BitSet lastpos, BitSet[] followpos) {
            LeftChild.ConstructPos(firstpos, lastpos, followpos);
            for (int pos = lastpos.NextSet(-1); pos != -1; pos = lastpos.NextSet(pos)) {
                followpos[pos].Or(firstpos);
            }
        }
        
        public override bool IsNullable {
            get { return LeftChild.IsNullable; }
        }

#if DEBUG
        public override void Dump(StringBuilder bb, SymbolsDictionary symbols, Positions positions) {
            LeftChild.Dump(bb, symbols, positions);
            bb.Append("+");
        }
#endif
    }
    
    sealed class QmarkNode : InteriorNode {
        public override void ConstructPos(BitSet firstpos, BitSet lastpos, BitSet[] followpos) {
            LeftChild.ConstructPos(firstpos, lastpos, followpos);
        }

        public override bool IsNullable {
            get { return true; }
        }

#if DEBUG
        public override void Dump(StringBuilder bb, SymbolsDictionary symbols, Positions positions) {
            LeftChild.Dump(bb, symbols, positions);
            bb.Append("?");
        }
#endif
    }

    sealed class StarNode : InteriorNode {
        public override void ConstructPos(BitSet firstpos, BitSet lastpos, BitSet[] followpos) {
            LeftChild.ConstructPos(firstpos, lastpos, followpos);
            for (int pos = lastpos.NextSet(-1); pos != -1; pos = lastpos.NextSet(pos)) {
                followpos[pos].Or(firstpos);
            }
        }
        
        public override bool IsNullable {
            get { return true; }
        }

#if DEBUG
        public override void Dump(StringBuilder bb, SymbolsDictionary symbols, Positions positions) {
            LeftChild.Dump(bb, symbols, positions);
            bb.Append("*");
        }
#endif
    }

#if EXPANDRANGE
    /// <summary>
    /// Temporary node to occurance range. Will be expended to a sequence of terminals
    /// </summary>
    sealed class RangeNode : InteriorNode {
        int min;
        int max;
        
        public RangeNode(int min, int max) {
            this.min = min;
            this.max = max;
        }

        public int Max {
            get { return max;}
        }

        public int Min {
            get { return min;}
        }

        public override SyntaxTreeNode Clone(Positions positions) {
            // range nodes have to be removed prior to that
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Expand tree will replace a{min, max} using following algorithm. Bare in mind that this sequence will have at least two leaves
        /// if min == 0 (max cannot be unbounded)
        ///         a?, ...  a?
        ///         \__     __/
        ///             max
        /// else
        ///     if max == unbounded
        ///         a,  ...   a, a*
        ///         \__     __/
        ///             min
        ///     else
        ///         a,  ...   a, a?,   ...     a?
        ///         \__     __/  \__          __/
        ///             min          max - min
        /// </summary>
        public override void ExpandTree(InteriorNode parent, SymbolsDictionary symbols, Positions positions) {
            LeftChild.ExpandTree(this, symbols, positions);
            SyntaxTreeNode replacementNode = null;
            if (min == 0) {
                Debug.Assert(max != int.MaxValue);
                replacementNode = NewQmark(LeftChild);
                for (int i = 0; i < max - 1; i ++) {
                    replacementNode = NewSequence(replacementNode, NewQmark(LeftChild.Clone(positions)));
                }
            }
            else {
                replacementNode = LeftChild;
                for (int i = 0; i < min - 1; i ++) {
                    replacementNode = NewSequence(replacementNode, LeftChild.Clone(positions));
                }
                if (max == int.MaxValue) {
                    replacementNode = NewSequence(replacementNode, NewStar(LeftChild.Clone(positions)));
                }
                else {
                    for (int i = 0; i < max - min; i ++) {
                        replacementNode = NewSequence(replacementNode, NewQmark(LeftChild.Clone(positions)));
                    }
                }
            }
            if (parent.LeftChild == this) {
                parent.LeftChild = replacementNode;
            }
            else {
                parent.RightChild = replacementNode;
            }
        }

        private SyntaxTreeNode NewSequence(SyntaxTreeNode leftChild, SyntaxTreeNode rightChild) {
            InteriorNode sequence = new SequenceNode();
            sequence.LeftChild = leftChild;
            sequence.RightChild = rightChild;
            return sequence;
        }

        private SyntaxTreeNode NewStar(SyntaxTreeNode leftChild) {
            InteriorNode star = new StarNode();
            star.LeftChild = leftChild;
            return star;
        }

        private SyntaxTreeNode NewQmark(SyntaxTreeNode leftChild) {
            InteriorNode qmark = new QmarkNode();
            qmark.LeftChild = leftChild;
            return qmark;
        }
        
        public override void ConstructPos(BitSet firstpos, BitSet lastpos, BitSet[] followpos) {
            throw new InvalidOperationException();
        }

        public override bool IsNullable {
            get { throw new InvalidOperationException(); }
        }

        public override void Dump(StringBuilder bb, SymbolsDictionary symbols, Positions positions) {
            LeftChild.Dump(bb, symbols, positions);
            bb.Append("{" + Convert.ToString(min, NumberFormatInfo.InvariantInfo) + ", " + Convert.ToString(max, NumberFormatInfo.InvariantInfo) + "}");
        }

    }
#endif


    /// <summary>
    /// Using range node as one of the terminals
    /// </summary>
    sealed class LeafRangeNode : LeafNode {
        decimal min;
        decimal max;
        BitSet nextIteration;

        public LeafRangeNode(decimal min, decimal max) : this(-1, min, max) {}

        public LeafRangeNode(int pos, decimal min, decimal max) : base(pos) {
            this.min = min;
            this.max = max;
        }

        public decimal Max {
            get { return max;}
        }

        public decimal Min {
            get { return min;}
        }

        public BitSet NextIteration {
            get {
                return nextIteration;
            }
            set {
                nextIteration = value;
            }
        }

        public override SyntaxTreeNode Clone(Positions positions) {
            return new LeafRangeNode(this.Pos, this.min, this.max);
        }
        
        public override bool IsRangeNode { 
            get {
                return true;
            }
        }

        public override void ExpandTree(InteriorNode parent, SymbolsDictionary symbols, Positions positions) {
            Debug.Assert(parent is SequenceNode);
            Debug.Assert(this == parent.RightChild);
            //change the range node min to zero if left is nullable
            if (parent.LeftChild.IsNullable) {
                min = 0;
            }
        }
    }

    #endregion

    #region ContentValidator
    /// <summary>
    /// Basic ContentValidator
    /// </summary>
    class ContentValidator {
        XmlSchemaContentType contentType;
        bool isOpen;  //For XDR Content Models or ANY
        bool isEmptiable;

        public static readonly ContentValidator Empty = new ContentValidator(XmlSchemaContentType.Empty);
        public static readonly ContentValidator TextOnly = new ContentValidator(XmlSchemaContentType.TextOnly, false, false);
        public static readonly ContentValidator Mixed = new ContentValidator(XmlSchemaContentType.Mixed);
        public static readonly ContentValidator Any = new ContentValidator(XmlSchemaContentType.Mixed, true, true);

        public ContentValidator(XmlSchemaContentType contentType) {
            this.contentType = contentType;
            this.isEmptiable = true;
        }
        
        protected ContentValidator(XmlSchemaContentType contentType, bool isOpen, bool isEmptiable) {
            this.contentType = contentType;
            this.isOpen = isOpen;
            this.isEmptiable = isEmptiable;
        }
        
        public XmlSchemaContentType ContentType { 
            get { return contentType; }
        }

        public bool PreserveWhitespace {
            get { return contentType == XmlSchemaContentType.TextOnly || contentType == XmlSchemaContentType.Mixed; }
        }

        public virtual bool IsEmptiable { 
            get { return isEmptiable; }
        }
        
        public bool IsOpen {
            get { 
                if (contentType == XmlSchemaContentType.TextOnly || contentType == XmlSchemaContentType.Empty)
                    return false;
                else
                    return isOpen; 
            }
            set { isOpen = value; }
        }

        public virtual void InitValidation(ValidationState context) {
            // do nothin'
        }

        public virtual object ValidateElement(XmlQualifiedName name, ValidationState context, out int errorCode) {
            if (contentType == XmlSchemaContentType.TextOnly || contentType == XmlSchemaContentType.Empty) { //Cannot have elements in TextOnly or Empty content
                context.NeedValidateChildren = false;
            }
            errorCode = -1;
            return null;
        }

        public virtual bool CompleteValidation(ValidationState context) {
            return true;
        }

        public virtual ArrayList ExpectedElements(ValidationState context, bool isRequiredOnly) {
            return null;
        }

        public virtual ArrayList ExpectedParticles(ValidationState context, bool isRequiredOnly, XmlSchemaSet schemaSet) {
            return null;
        }

        public static void AddParticleToExpected(XmlSchemaParticle p, XmlSchemaSet schemaSet, ArrayList particles) {
            AddParticleToExpected(p, schemaSet, particles, false);
        }

        public static void AddParticleToExpected(XmlSchemaParticle p, XmlSchemaSet schemaSet, ArrayList particles, bool global) {
            if (!particles.Contains(p)) {
                particles.Add(p);
            }
            //Only then it can be head of substitutionGrp, if it is, add its members 
            XmlSchemaElement elem = p as XmlSchemaElement;
            if (elem != null && (global ||!elem.RefName.IsEmpty)) { 
				XmlSchemaObjectTable substitutionGroups = schemaSet.SubstitutionGroups;
                XmlSchemaSubstitutionGroup grp = (XmlSchemaSubstitutionGroup)substitutionGroups[elem.QualifiedName];
                if (grp != null) {
                    //Grp members wil contain the head as well, so filter head as we added it already
                    for (int i = 0; i < grp.Members.Count; ++i) {
                        XmlSchemaElement member = (XmlSchemaElement)grp.Members[i];
                        if (!elem.QualifiedName.Equals(member.QualifiedName) && !particles.Contains(member)) { //A member might have been directly present as an element in the content model
                            particles.Add(member);
                        }
                    }
                }
            }
        }
    }

    sealed class ParticleContentValidator : ContentValidator {
        SymbolsDictionary symbols;          
        Positions positions;                
        Stack stack;                        // parsing context
        SyntaxTreeNode contentNode;         // content model points to syntax tree
        bool isPartial;                     // whether the closure applies to partial or the whole node that is on top of the stack
        int minMaxNodesCount;
        bool enableUpaCheck;

        public ParticleContentValidator(XmlSchemaContentType contentType) : this(contentType, true) {
        }

        public ParticleContentValidator(XmlSchemaContentType contentType, bool enableUpaCheck) : base(contentType) {
            this.enableUpaCheck = enableUpaCheck;
        }

        public override void InitValidation(ValidationState context) {
            // ParticleContentValidator cannot be used during validation
            throw new InvalidOperationException();
        }

        public override object ValidateElement(XmlQualifiedName name, ValidationState context, out int errorCode) {
            // ParticleContentValidator cannot be used during validation
            throw new InvalidOperationException();
        }

        public override bool CompleteValidation(ValidationState context) {
            // ParticleContentValidator cannot be used during validation
            throw new InvalidOperationException();
        }

        public void Start() {
            symbols = new SymbolsDictionary();
            positions = new Positions();
            stack = new Stack();
        }

        public void OpenGroup() {
            stack.Push(null);
        }

        public void CloseGroup() {
            SyntaxTreeNode node = (SyntaxTreeNode)stack.Pop();
            if (node == null) {
                return;
            }
            if (stack.Count == 0) {
                contentNode = node;
                isPartial = false;
            }
            else {
                // some collapsing to do...
                InteriorNode inNode = (InteriorNode)stack.Pop();
                if (inNode != null) {
                    inNode.RightChild = node;
                    node = inNode;
                    isPartial = true;
                }
                else {
                    isPartial = false;
                }
                stack.Push(node);
            }
        }
        
        public bool Exists(XmlQualifiedName name) {
            if (symbols.Exists(name)) {
                return true;
            }
            return false;
        }

        public void AddName(XmlQualifiedName name, object particle) {
            AddLeafNode(new LeafNode(positions.Add(symbols.AddName(name, particle), particle)));
        }
        
        public void AddNamespaceList(NamespaceList namespaceList, object particle) {
            symbols.AddNamespaceList(namespaceList, particle, false);
            AddLeafNode(new NamespaceListNode(namespaceList, particle));
        }

        private void AddLeafNode(SyntaxTreeNode node) {
            if (stack.Count > 0) {
                InteriorNode inNode = (InteriorNode)stack.Pop();
                if (inNode != null) {
                    inNode.RightChild = node;
                    node = inNode;
                }
            }
            stack.Push( node );
            isPartial = true;
        }

        public void AddChoice() {
            SyntaxTreeNode node = (SyntaxTreeNode)stack.Pop();
            InteriorNode choice = new ChoiceNode();
            choice.LeftChild = node;
            stack.Push(choice);
        }

        public void AddSequence() {
            SyntaxTreeNode node = (SyntaxTreeNode)stack.Pop();
            InteriorNode sequence = new SequenceNode();
            sequence.LeftChild = node;
            stack.Push(sequence);
        }

        public void AddStar() {
            Closure(new StarNode());
        }

        public void AddPlus() {
            Closure(new PlusNode());
        }

        public void AddQMark() {
            Closure(new QmarkNode());
        }

        public void AddLeafRange(decimal min, decimal max) {
            LeafRangeNode rNode = new LeafRangeNode(min, max);
            int pos = positions.Add(-2, rNode);
            rNode.Pos = pos;

            InteriorNode sequence = new SequenceNode();
            sequence.RightChild = rNode;
            Closure(sequence);
            minMaxNodesCount++;
        }

#if EXPANDRANGE
        public void AddRange(int min, int max) {
            Closure(new RangeNode(min, max));
        }
#endif
        private void Closure(InteriorNode node) {
            if (stack.Count > 0) {
                SyntaxTreeNode topNode = (SyntaxTreeNode)stack.Pop();
                InteriorNode inNode = topNode as InteriorNode;
                if (isPartial && inNode != null) {
                    // need to reach in and wrap right hand side of element.
                    // and n remains the same.
                    node.LeftChild = inNode.RightChild;
                    inNode.RightChild = node;
                }
                else {
                    // wrap terminal or any node
                    node.LeftChild = topNode;
                    topNode = node;
                }
                stack.Push(topNode);
            }
            else if (contentNode != null) { //If there is content to wrap
                // wrap whole content
                node.LeftChild = contentNode;
                contentNode = node;
            }
        }

        public ContentValidator Finish() {
            return Finish(true);
        }

        public ContentValidator Finish(bool useDFA) {
            Debug.Assert(ContentType == XmlSchemaContentType.ElementOnly || ContentType == XmlSchemaContentType.Mixed);
            if (contentNode == null) {
                if (ContentType == XmlSchemaContentType.Mixed) {
                    string ctype = IsOpen ? "Any" : "TextOnly";
                    Debug.WriteLineIf(DiagnosticsSwitches.XmlSchemaContentModel.Enabled,  "\t\t\tContentType:  " + ctype);
                    return IsOpen ? ContentValidator.Any : ContentValidator.TextOnly;
                }
                else {
                    Debug.WriteLineIf(DiagnosticsSwitches.XmlSchemaContentModel.Enabled,  "\t\t\tContent:   EMPTY");
                    Debug.Assert(!IsOpen);
                    return ContentValidator.Empty;
                }
            }

            #if DEBUG
            StringBuilder bb = new StringBuilder();
            contentNode.Dump(bb, symbols, positions);
            Debug.WriteLineIf(DiagnosticsSwitches.XmlSchemaContentModel.Enabled,  "\t\t\tContent :   " + bb.ToString());
            #endif

            // Add end marker
            InteriorNode contentRoot = new SequenceNode();
            contentRoot.LeftChild = contentNode;
            LeafNode endMarker = new LeafNode(positions.Add(symbols.AddName(XmlQualifiedName.Empty, null), null));
            contentRoot.RightChild = endMarker;

            // Eliminate NamespaceListNode(s) and RangeNode(s)
            contentNode.ExpandTree(contentRoot, symbols, positions);

            #if DEBUG
            bb = new StringBuilder();
            contentRoot.LeftChild.Dump(bb, symbols, positions);
            Debug.WriteLineIf(DiagnosticsSwitches.XmlSchemaContentModel.Enabled,  "\t\t\tExpended:   " + bb.ToString());
            #endif

            // calculate followpos
            int symbolsCount = symbols.Count;
            int positionsCount = positions.Count;
            BitSet firstpos = new BitSet(positionsCount);
            BitSet lastpos = new BitSet(positionsCount);
            BitSet[] followpos = new BitSet[positionsCount];
            for (int i = 0; i < positionsCount; i++) {
                followpos[i] = new BitSet(positionsCount);
            }
            contentRoot.ConstructPos(firstpos, lastpos, followpos);
#if DEBUG
            Debug.WriteLineIf(DiagnosticsSwitches.XmlSchemaContentModel.Enabled, "firstpos, lastpos, followpos");
            SequenceNode.WritePos(firstpos, lastpos, followpos);
#endif
            if (minMaxNodesCount > 0) { //If the tree has any terminal range nodes
                BitSet positionsWithRangeTerminals;
                BitSet[] minMaxFollowPos = CalculateTotalFollowposForRangeNodes(firstpos, followpos, out positionsWithRangeTerminals);
                
                if (enableUpaCheck) {
                    CheckCMUPAWithLeafRangeNodes(GetApplicableMinMaxFollowPos(firstpos, positionsWithRangeTerminals, minMaxFollowPos));
                    for (int i = 0; i < positionsCount; i++) {
                        CheckCMUPAWithLeafRangeNodes(GetApplicableMinMaxFollowPos(followpos[i], positionsWithRangeTerminals, minMaxFollowPos));
                    }
                }
                return new RangeContentValidator(firstpos, followpos, symbols, positions, endMarker.Pos, this.ContentType, contentRoot.LeftChild.IsNullable, positionsWithRangeTerminals, minMaxNodesCount); 
            }
            else {
                int[][] transitionTable = null;
                // if each symbol has unique particle we are golden
                if (!symbols.IsUpaEnforced) {
                    if (enableUpaCheck) {
                        // multiple positions that match the same symbol have different particles, but they never follow the same position
                        CheckUniqueParticleAttribution(firstpos, followpos);
                    }
                }
                else if (useDFA) {
                    // Can return null if the number of states reaches higher than 8192 / positionsCount
                    transitionTable = BuildTransitionTable(firstpos, followpos, endMarker.Pos); 
                }
                #if DEBUG
                bb = new StringBuilder();
                Dump(bb, followpos, transitionTable);    
                Debug.WriteLineIf(DiagnosticsSwitches.XmlSchemaContentModel.Enabled, bb.ToString());
                #endif
                if (transitionTable != null) {
                    return new DfaContentValidator(transitionTable, symbols,this.ContentType, this.IsOpen, contentRoot.LeftChild.IsNullable);
                } else {
                    return new NfaContentValidator(firstpos, followpos, symbols, positions, endMarker.Pos, this.ContentType, this.IsOpen, contentRoot.LeftChild.IsNullable);
                }
            }
        }

        private BitSet[] CalculateTotalFollowposForRangeNodes(BitSet firstpos, BitSet[] followpos, out BitSet posWithRangeTerminals) {
            int positionsCount = positions.Count; //terminals
            posWithRangeTerminals = new BitSet(positionsCount);
            
            //Compute followpos for each range node
            //For any range node that is surrounded by an outer range node, its follow positions will include those of the outer range node
            BitSet[] minmaxFollowPos = new BitSet[minMaxNodesCount];
            int localMinMaxNodesCount = 0;
            
            for (int i = positionsCount - 1;  i >= 0; i--) { 
                Position p = positions[i];
                if (p.symbol == -2) { //P is a LeafRangeNode
                    LeafRangeNode lrNode = p.particle as LeafRangeNode;
                    Debug.Assert(lrNode != null);
                    BitSet tempFollowPos = new BitSet(positionsCount);
                    tempFollowPos.Clear();
                    tempFollowPos.Or(followpos[i]); //Add the followpos of the range node
                    if (lrNode.Min != lrNode.Max) { //If they are the same, then followpos cannot include the firstpos
                        tempFollowPos.Or(lrNode.NextIteration); //Add the nextIteration of the range node (this is the firstpos of its parent's leftChild)
                    }

                    //For each position in the bitset, if it is a outer range node (pos > i), then add its followpos as well to the current node's followpos
                    for (int pos = tempFollowPos.NextSet(-1); pos != -1; pos = tempFollowPos.NextSet(pos)) {
                        if (pos > i) {
                            Position p1 = positions[pos];
                            if (p1.symbol == -2) {
                                LeafRangeNode lrNode1 = p1.particle as LeafRangeNode;
                                Debug.Assert(lrNode1 != null);
                                tempFollowPos.Or(minmaxFollowPos[lrNode1.Pos]);
                            }
                        }
                    }
                    //set the followpos built to the index in the BitSet[]
                    minmaxFollowPos[localMinMaxNodesCount] = tempFollowPos; 
                    lrNode.Pos = localMinMaxNodesCount++; 
                    posWithRangeTerminals.Set(i);
                }
            }
            return minmaxFollowPos;
        }

        private void CheckCMUPAWithLeafRangeNodes(BitSet curpos) {
            object[] symbolMatches = new object[symbols.Count];
            for (int pos = curpos.NextSet(-1); pos != -1; pos = curpos.NextSet(pos)) {
                Position currentPosition = positions[pos];
                int symbol = currentPosition.symbol;
                if (symbol >= 0) { //its not a range position
                    if (symbolMatches[symbol] != null) {
                        throw new UpaException(symbolMatches[symbol], currentPosition.particle);
                    }
                    else {
                        symbolMatches[symbol] = currentPosition.particle;
                    }
                }
            }
        }

        //For each position, this method calculates the additional follows of any range nodes that need to be added to its followpos
        //((ab?)2-4)c, Followpos of a is b as well as that of node R(2-4) = c
        private BitSet GetApplicableMinMaxFollowPos(BitSet curpos, BitSet posWithRangeTerminals, BitSet[] minmaxFollowPos) {
            if (curpos.Intersects(posWithRangeTerminals)) {
                BitSet newSet = new BitSet(positions.Count); //Doing work again 
                newSet.Or(curpos);
                newSet.And(posWithRangeTerminals);
                curpos = curpos.Clone();
                for (int pos = newSet.NextSet(-1); pos != -1; pos = newSet.NextSet(pos)) {
                    LeafRangeNode lrNode = positions[pos].particle as LeafRangeNode;
                    curpos.Or(minmaxFollowPos[lrNode.Pos]);
                }
            }
            return curpos;
        }

        private void CheckUniqueParticleAttribution(BitSet firstpos, BitSet[] followpos) {
            CheckUniqueParticleAttribution(firstpos);
            for (int i = 0; i < positions.Count; i++) {
                CheckUniqueParticleAttribution(followpos[i]);
            }
        }

        private void CheckUniqueParticleAttribution(BitSet curpos) {
            // particles will be attributed uniquely if the same symbol never poins to two different ones
            object[] particles = new object[symbols.Count]; 
            for (int pos = curpos.NextSet(-1); pos != -1; pos = curpos.NextSet(pos)) {
                // if position can follow
                int symbol = positions[pos].symbol;
                if (particles[symbol] == null) {
                    // set particle for the symbol
                    particles[symbol] = positions[pos].particle;
                }
                else if (particles[symbol] != positions[pos].particle) {
                    throw new UpaException(particles[symbol], positions[pos].particle);
                }
                // two different position point to the same symbol and particle - that's OK
            }
        }

        /// <summary>
        /// Algorithm 3.5 Construction of a DFA from a regular expression
        /// </summary>
        private int[][] BuildTransitionTable(BitSet firstpos, BitSet[] followpos, int endMarkerPos) {
            const int TimeConstant = 8192; //(MaxStates * MaxPositions should be a constant) 
            int positionsCount = positions.Count;
            int MaxStatesCount = TimeConstant / positionsCount;
            int symbolsCount = symbols.Count;
            
            // transition table (Dtran in the book)
            ArrayList transitionTable = new ArrayList();
            
            // state lookup table (Dstate in the book)
            Hashtable stateTable = new Hashtable();
            
            // Add empty set that would signal an error
            stateTable.Add(new BitSet(positionsCount), -1);

            // lists unmarked states
            Queue unmarked = new Queue();

            // initially, the only unmarked state in Dstates is firstpo(root) 
            int state = 0;
            unmarked.Enqueue(firstpos);
            stateTable.Add(firstpos, 0);
            transitionTable.Add(new int[symbolsCount + 1]);

            // while there is an umnarked state T in Dstates do begin
            while (unmarked.Count > 0) {
                BitSet statePosSet = (BitSet)unmarked.Dequeue(); // all positions that constitute DFA state 
                Debug.Assert(state == (int)stateTable[statePosSet]); // just make sure that statePosSet is for correct state
                int[] transition = (int[])transitionTable[state];
                if (statePosSet[endMarkerPos]) {
                    transition[symbolsCount] = 1;   // accepting
                }

                // for each input symbol a do begin
                for (int symbol = 0; symbol < symbolsCount; symbol ++) {
                    // let U be the set of positions that are in followpos(p)
                    //       for some position p in T
                    //       such that the symbol at position p is a
                    BitSet newset = new BitSet(positionsCount);
                    for (int pos = statePosSet.NextSet(-1); pos != -1; pos = statePosSet.NextSet(pos)) {
                        if (symbol == positions[pos].symbol) {
                            newset.Or(followpos[pos]);
                        }
                    }

                    // if U is not empty and is not in Dstates then
                    //      add U as an unmarked state to Dstates
                    object lookup = stateTable[newset];
                    if (lookup != null) {
                        transition[symbol] = (int)lookup;
                    }
                    else {
                        // construct new state
                        int newState = stateTable.Count - 1;
                        if (newState >= MaxStatesCount) {
                            return null;
                        }
                        unmarked.Enqueue(newset);
                        stateTable.Add(newset, newState);
                        transitionTable.Add(new int[symbolsCount + 1]);
                        transition[symbol] = newState;
                    }
                }
                state++;
            }
            // now convert transition table to array
            return (int[][])transitionTable.ToArray(typeof(int[]));
        }

#if DEBUG
        private void Dump(StringBuilder bb, BitSet[] followpos, int[][] transitionTable) {
            // Temporary printout
            bb.AppendLine("Positions");
            for (int i = 0; i < positions.Count; i ++) {
                bb.AppendLine(i + " " + positions[i].symbol.ToString(NumberFormatInfo.InvariantInfo) + " " + symbols.NameOf(positions[i].symbol));
            }
            bb.AppendLine("Followpos");
            for (int i = 0; i < positions.Count; i++) {
                for (int j = 0; j < positions.Count; j++) {
                    bb.Append(followpos[i][j] ? "X" : "O");
                }
               bb.AppendLine();
            }
            if (transitionTable != null) {
                // Temporary printout
                bb.AppendLine("Transitions");
                for (int i = 0; i < transitionTable.Length; i++) {
                    for (int j = 0; j < symbols.Count; j++) {
                        if (transitionTable[i][j] == -1) {
                            bb.Append("  x  ");
                        }
                        else {
                            bb.AppendFormat(" {0:000} ", transitionTable[i][j]);
                        }
                    }
                    bb.AppendLine(transitionTable[i][symbols.Count] == 1 ? "+" : "");
                }
            }
        }
#endif
    }

    /// <summary>
    /// Deterministic Finite Automata
    /// Compilers by Aho, Sethi, Ullman.
    /// ISBN 0-201-10088-6, pp. 115, 116, 140 
    /// </summary>
    sealed class DfaContentValidator : ContentValidator {
        int[][] transitionTable;
        SymbolsDictionary symbols;

        /// <summary>
        /// Algorithm 3.5 Construction of a DFA from a regular expression
        /// </summary>
        internal DfaContentValidator(
            int[][] transitionTable, SymbolsDictionary symbols,
            XmlSchemaContentType contentType, bool isOpen, bool isEmptiable)  : base(contentType, isOpen, isEmptiable) {
            this.transitionTable = transitionTable;
            this.symbols = symbols;
        }

        public override void InitValidation(ValidationState context) {
            context.CurrentState.State = 0;
            context.HasMatched = transitionTable[0][symbols.Count] > 0;
        }

        /// <summary>
        /// Algorithm 3.1 Simulating a DFA
        /// </summary>
        public override object ValidateElement(XmlQualifiedName name, ValidationState context, out int errorCode) {
            int symbol = symbols[name];
            int state = transitionTable[context.CurrentState.State][symbol];
            errorCode = 0;
            if (state != -1) {
                context.CurrentState.State = state;
                context.HasMatched = transitionTable[context.CurrentState.State][symbols.Count] > 0;
                return symbols.GetParticle(symbol); // OK
            }
            if (IsOpen && context.HasMatched) {
                // XDR allows any well-formed contents after matched.
                return null;
            }
            context.NeedValidateChildren = false;
            errorCode = -1;
            return null; // will never be here
        }

        public override bool CompleteValidation(ValidationState context) {
            if (!context.HasMatched) {
                return false;
            }
            return true;
        }

        public override ArrayList ExpectedElements(ValidationState context, bool isRequiredOnly) {
            ArrayList names = null;
            int[] transition = transitionTable[context.CurrentState.State];
            if (transition != null) {
                for (int i = 0; i < transition.Length - 1; i ++) {
                    if (transition[i] != -1) {
                        if (names == null) {
                            names = new ArrayList();
                        }
                        XmlSchemaParticle p = (XmlSchemaParticle)symbols.GetParticle(i);
                        if (p == null) {
                            string s = symbols.NameOf(i);
                            if (s.Length != 0) {
                                names.Add(s);
                            }
                        }
                        else {
                            string s = p.NameString;
                            if (!names.Contains(s)) {
                                names.Add(s);
                            }
                        }
                    }
                }
            }
            return names;
        }

        public override ArrayList ExpectedParticles(ValidationState context, bool isRequiredOnly, XmlSchemaSet schemaSet) {
            ArrayList particles = new ArrayList();
            int[] transition = transitionTable[context.CurrentState.State];
            if (transition != null) {
                for (int i = 0; i < transition.Length - 1; i ++) {
                    if (transition[i] != -1) {
                        XmlSchemaParticle p = (XmlSchemaParticle)symbols.GetParticle(i);
                        if (p == null) {
                            continue;
                        }
                        AddParticleToExpected(p, schemaSet, particles);
                    }
                }
            }
            return particles;
        }        
    }

    /// <summary>
    /// Nondeterministic Finite Automata
    /// Compilers by Aho, Sethi, Ullman.
    /// ISBN 0-201-10088-6, pp. 126,140 
    /// </summary>
    sealed class NfaContentValidator : ContentValidator {
        BitSet firstpos;
        BitSet[] followpos;
        SymbolsDictionary symbols;
        Positions positions;
        int endMarkerPos;

        internal NfaContentValidator(
            BitSet firstpos, BitSet[] followpos, SymbolsDictionary symbols, Positions positions, int endMarkerPos,
            XmlSchemaContentType contentType, bool isOpen, bool isEmptiable)  : base(contentType, isOpen, isEmptiable) {
            this.firstpos = firstpos;
            this.followpos = followpos;
            this.symbols = symbols;
            this.positions = positions;
            this.endMarkerPos = endMarkerPos;
        }

        public override void InitValidation(ValidationState context) {
            context.CurPos[0] = firstpos.Clone();
            context.CurPos[1] = new BitSet(firstpos.Count);
            context.CurrentState.CurPosIndex = 0;
        }

        /// <summary>
        /// Algorithm 3.4 Simulation of an NFA
        /// </summary>
        public override object ValidateElement(XmlQualifiedName name, ValidationState context, out int errorCode) {
            BitSet curpos = context.CurPos[context.CurrentState.CurPosIndex];
            int next = (context.CurrentState.CurPosIndex + 1) % 2;
            BitSet nextpos = context.CurPos[next];
            nextpos.Clear();
            int symbol = symbols[name];
            object particle = null;
            errorCode = 0;
            for (int pos = curpos.NextSet(-1); pos != -1; pos = curpos.NextSet(pos)) {
                // if position can follow
                if (symbol == positions[pos].symbol) {
                    nextpos.Or(followpos[pos]);
                    particle = positions[pos].particle; //Between element and wildcard, element will be in earlier pos than wildcard since we add the element nodes to the list of positions first
                    break;                              // and then ExpandTree for the namespace nodes which adds the wildcards to the positions list
                }
            }
            if (!nextpos.IsEmpty) {
                context.CurrentState.CurPosIndex = next;
                return particle;
            }
            if (IsOpen && curpos[endMarkerPos]) {
                // XDR allows any well-formed contents after matched.
                return null;
            }
            context.NeedValidateChildren = false;
            errorCode = -1;
            return null; // will never be here

        }

#if FINDUPA_PARTICLE
        private bool FindUPAParticle(ref object originalParticle, object newParticle) {
            if (originalParticle == null) { 
                originalParticle = newParticle;
                if (originalParticle is XmlSchemaElement) { //if the first particle is element, then break, otherwise try to find an element
                    return true;
                }
            }
            else if (newParticle is XmlSchemaElement) {
                if (originalParticle is XmlSchemaAny) { //Weak wildcards, element takes precendence over any
                    originalParticle = newParticle;
                    return true;
                }
            }
            return false;
        }
#endif

        public override bool CompleteValidation(ValidationState context) {
            if (!context.CurPos[context.CurrentState.CurPosIndex][endMarkerPos]) {
                return false;
            }
            return true;
        }

        public override ArrayList ExpectedElements(ValidationState context, bool isRequiredOnly) {
            ArrayList names = null;
            BitSet curpos = context.CurPos[context.CurrentState.CurPosIndex];
            for (int pos = curpos.NextSet(-1); pos != -1; pos = curpos.NextSet(pos)) {
                if (names == null) {
                    names = new ArrayList();
                }
                XmlSchemaParticle p = (XmlSchemaParticle)positions[pos].particle;
                if (p == null) {
                    string s = symbols.NameOf(positions[pos].symbol);
                    if (s.Length != 0) {
                        names.Add(s);
                    }
                }
                else {
                    string s = p.NameString;
                    if (!names.Contains(s)) {
                        names.Add(s);
                    }
                }
            }
            return names;
        }

        public override ArrayList ExpectedParticles(ValidationState context, bool isRequiredOnly, XmlSchemaSet schemaSet) {
            ArrayList particles = new ArrayList();
            BitSet curpos = context.CurPos[context.CurrentState.CurPosIndex];
            for (int pos = curpos.NextSet(-1); pos != -1; pos = curpos.NextSet(pos)) {
                XmlSchemaParticle p = (XmlSchemaParticle)positions[pos].particle;
                if (p == null) {
                    continue;
                }
                else {
                    AddParticleToExpected(p, schemaSet, particles);
                }
            }
            return particles;
        }
    }

    internal struct RangePositionInfo {
        public BitSet curpos;            
        public decimal[] rangeCounters;
    }
    
    sealed class RangeContentValidator : ContentValidator {
        BitSet firstpos;
        BitSet[] followpos;
        BitSet positionsWithRangeTerminals;
        SymbolsDictionary symbols;
        Positions positions;
        int minMaxNodesCount;    
        int endMarkerPos;

        internal RangeContentValidator(
            BitSet firstpos, BitSet[] followpos, SymbolsDictionary symbols, Positions positions, int endMarkerPos, XmlSchemaContentType contentType, bool isEmptiable, BitSet positionsWithRangeTerminals, int minmaxNodesCount)  : base(contentType, false, isEmptiable) {
            this.firstpos = firstpos;
            this.followpos = followpos;
            this.symbols = symbols;
            this.positions = positions;
            this.positionsWithRangeTerminals = positionsWithRangeTerminals;
            this.minMaxNodesCount = minmaxNodesCount;
            this.endMarkerPos = endMarkerPos;
        }

        public override void InitValidation(ValidationState context) {
            int positionsCount = positions.Count;
            List<RangePositionInfo> runningPositions = context.RunningPositions;
            if (runningPositions != null) {
                Debug.Assert(minMaxNodesCount != 0);
                runningPositions.Clear();
            }
            else {
                runningPositions = new List<RangePositionInfo>();
                context.RunningPositions = runningPositions;
            }
            RangePositionInfo rposInfo = new RangePositionInfo();
            rposInfo.curpos = firstpos.Clone();

            rposInfo.rangeCounters = new decimal[minMaxNodesCount];
            runningPositions.Add(rposInfo);
            context.CurrentState.NumberOfRunningPos = 1;
            context.HasMatched = rposInfo.curpos.Get(endMarkerPos);
        }

        public override object ValidateElement(XmlQualifiedName name, ValidationState context, out int errorCode) {
            errorCode = 0;
            int symbol = symbols[name];
            bool hasSeenFinalPosition = false;
            List<RangePositionInfo> runningPositions = context.RunningPositions;
            int matchCount = context.CurrentState.NumberOfRunningPos;
            int k = 0; 
            RangePositionInfo rposInfo;
            
            int pos = -1;
            int firstMatchedIndex = -1;
            bool matched = false;

#if RANGE_DEBUG
            WriteRunningPositions("Current running positions to match", runningPositions, name, matchCount);
#endif

            while (k < matchCount) { //we are looking for the first match in the list of bitsets
                rposInfo = runningPositions[k];
                BitSet curpos = rposInfo.curpos;
                for (int matchpos = curpos.NextSet(-1); matchpos != -1; matchpos = curpos.NextSet(matchpos)) { //In all sets, have to scan all positions because of Disabled UPA possibility
                    if (symbol == positions[matchpos].symbol) {
                        pos = matchpos; 
                        if (firstMatchedIndex == -1) { // get the first match for this symbol
                            firstMatchedIndex = k;
                        }
                        matched = true;
                        break;
                    }
                }
                if (matched && positions[pos].particle is XmlSchemaElement) { //We found a match in the list, break at that bitset
                    break;
                }
                else {
                    k++;
                }
            }

            if (k == matchCount && pos != -1) { // we did find a match but that was any and hence continued ahead for element
                k = firstMatchedIndex;
            }
            if (k < matchCount) { //There is a match
                if (k != 0) { //If the first bitset itself matched, then no need to remove anything
#if RANGE_DEBUG
                    WriteRunningPositions("Removing unmatched entries till the first match", runningPositions, XmlQualifiedName.Empty, k);
#endif
                    runningPositions.RemoveRange(0, k); //Delete entries from 0 to k-1
                }
                matchCount = matchCount - k;
                k = 0; // Since we re-sized the array
                while (k < matchCount) {
                    rposInfo = runningPositions[k];
                    matched = rposInfo.curpos.Get(pos); //Look for the bitset that matches the same position as pos
                    if (matched) { //If match found, get the follow positions of the current matched position
#if RANGE_DEBUG
                        Debug.WriteIf(DiagnosticsSwitches.XmlSchemaContentModel.Enabled, "Matched position: " + pos + " "); SequenceNode.WriteBitSet(rposInfo.curpos);
                        Debug.WriteIf(DiagnosticsSwitches.XmlSchemaContentModel.Enabled, "Follow pos of Matched position: "); SequenceNode.WriteBitSet(followpos[pos]);
#endif
                        rposInfo.curpos = followpos[pos]; //Note that we are copying the same counters of the current position to that of the follow position
                        runningPositions[k] = rposInfo; 
                        k++;
                    }
                    else { //Clear the current pos and get another position from the list to start matching
                        matchCount--;
                        if (matchCount > 0) {
                            RangePositionInfo lastrpos = runningPositions[matchCount];
                            runningPositions[matchCount] = runningPositions[k];
                            runningPositions[k] = lastrpos;
                        }
                    }
                }
            }
            else { //There is no match
                matchCount = 0;
            }

            if (matchCount > 0) {
                Debug.Assert(minMaxNodesCount > 0);
                if (matchCount >= 10000) {
                    context.TooComplex = true;
                    matchCount /= 2;
                }
#if RANGE_DEBUG
                WriteRunningPositions("Matched positions to expand ", runningPositions, name, matchCount);
#endif

                for (k = matchCount - 1; k >= 0; k--) { 
                    int j = k;
                    BitSet currentRunningPosition = runningPositions[k].curpos;
                    hasSeenFinalPosition = hasSeenFinalPosition || currentRunningPosition.Get(endMarkerPos); //Accepting position reached if the current position BitSet contains the endPosition
                    while (matchCount < 10000 && currentRunningPosition.Intersects(positionsWithRangeTerminals)) {
                        //Now might add 2 more positions to followpos 
                        //1. nextIteration of the rangeNode, which is firstpos of its parent's leftChild
                        //2. Followpos of the range node

                        BitSet countingPosition = currentRunningPosition.Clone();
                        countingPosition.And(positionsWithRangeTerminals);
                        int cPos = countingPosition.NextSet(-1); //Get the first position where leaf range node appears
                        LeafRangeNode lrNode = positions[cPos].particle as LeafRangeNode; //For a position with leaf range node, the particle is the node itself
                        Debug.Assert(lrNode != null);

                        rposInfo = runningPositions[j];
                        if (matchCount + 2 >= runningPositions.Count) {
                            runningPositions.Add(new RangePositionInfo());
                            runningPositions.Add(new RangePositionInfo());
                        }
                        RangePositionInfo newRPosInfo = runningPositions[matchCount];
                        if (newRPosInfo.rangeCounters == null) {
                            newRPosInfo.rangeCounters = new decimal[minMaxNodesCount];
                        }
                        Array.Copy(rposInfo.rangeCounters, 0, newRPosInfo.rangeCounters, 0, rposInfo.rangeCounters.Length);
                        decimal count = ++newRPosInfo.rangeCounters[lrNode.Pos];

                        if (count == lrNode.Max) {
                            newRPosInfo.curpos = followpos[cPos]; //since max has been reached, Get followposition of range node
                            newRPosInfo.rangeCounters[lrNode.Pos] = 0; //reset counter
                            runningPositions[matchCount] = newRPosInfo;
                            j = matchCount++;
                        }
                        else if (count < lrNode.Min) {
                            newRPosInfo.curpos = lrNode.NextIteration;
                            runningPositions[matchCount] = newRPosInfo;
                            matchCount++;
                            break;
                        }
                        else { // min <= count < max
                            newRPosInfo.curpos = lrNode.NextIteration; //set currentpos to firstpos of node which has the range
                            runningPositions[matchCount] = newRPosInfo;
                            j = matchCount + 1;
                            newRPosInfo = runningPositions[j];
                            if (newRPosInfo.rangeCounters == null) {
                                newRPosInfo.rangeCounters = new decimal[minMaxNodesCount];
                            }
                            Array.Copy(rposInfo.rangeCounters, 0, newRPosInfo.rangeCounters, 0, rposInfo.rangeCounters.Length);
                            newRPosInfo.curpos = followpos[cPos];
                            newRPosInfo.rangeCounters[lrNode.Pos] = 0;
                            runningPositions[j] = newRPosInfo;
                            matchCount += 2;
                        }
                        currentRunningPosition = runningPositions[j].curpos;
                        hasSeenFinalPosition = hasSeenFinalPosition || currentRunningPosition.Get(endMarkerPos);
                    }
                }
                context.HasMatched = hasSeenFinalPosition;
                context.CurrentState.NumberOfRunningPos = matchCount;
                return positions[pos].particle;
            } //matchcount > 0
            errorCode = -1;
            context.NeedValidateChildren = false;
            return null;
        }

#if RANGE_DEBUG
        private void WriteRunningPositions(string text, List<RangePositionInfo> runningPositions, XmlQualifiedName name, int length) {
            int counter = 0;
            Debug.WriteLineIf(DiagnosticsSwitches.XmlSchemaContentModel.Enabled, "");
            Debug.WriteLineIf(DiagnosticsSwitches.XmlSchemaContentModel.Enabled, "");
            Debug.WriteLineIf(DiagnosticsSwitches.XmlSchemaContentModel.Enabled, text + name.Name);
            while (counter < length) {
                BitSet curpos = runningPositions[counter].curpos;
                SequenceNode.WriteBitSet(curpos);
                for(int rcnt = 0; rcnt < runningPositions[counter].rangeCounters.Length; rcnt++) {
                    Debug.WriteIf(DiagnosticsSwitches.XmlSchemaContentModel.Enabled, "RangeCounter[" + rcnt + "]" + runningPositions[counter].rangeCounters[rcnt] + " ");                    
                }
                Debug.WriteLineIf(DiagnosticsSwitches.XmlSchemaContentModel.Enabled, "");
                Debug.WriteLineIf(DiagnosticsSwitches.XmlSchemaContentModel.Enabled, "");
                counter++;
            }
        }
#endif
        public override bool CompleteValidation(ValidationState context) {
            return context.HasMatched;
        }

        public override ArrayList ExpectedElements(ValidationState context, bool isRequiredOnly) {
            ArrayList names = null;
            BitSet expectedPos;
            if (context.RunningPositions != null) {
                List<RangePositionInfo> runningPositions = context.RunningPositions;
                expectedPos = new BitSet(positions.Count);
                for (int i = context.CurrentState.NumberOfRunningPos - 1; i >=0; i--) {
                    Debug.Assert(runningPositions[i].curpos != null);
                    expectedPos.Or(runningPositions[i].curpos);
                }
                for (int pos = expectedPos.NextSet(-1); pos != -1; pos = expectedPos.NextSet(pos)) {
                    if (names == null) {
                        names = new ArrayList();
                    }
                    int symbol = positions[pos].symbol;
                    if (symbol >= 0) { //non range nodes
                        XmlSchemaParticle p = positions[pos].particle as XmlSchemaParticle;
                        if (p == null) {
                            string s = symbols.NameOf(positions[pos].symbol);
                            if (s.Length != 0) {
                                names.Add(s);
                            }
                        }
                        else {
                            string s = p.NameString;
                            if (!names.Contains(s)) {
                                names.Add(s);
                            }
                        }
                    }
                }
            }
            return names;
        }

        public override ArrayList ExpectedParticles(ValidationState context, bool isRequiredOnly, XmlSchemaSet schemaSet) {
            ArrayList particles = new ArrayList();
            BitSet expectedPos;
            if (context.RunningPositions != null) {
                List<RangePositionInfo> runningPositions = context.RunningPositions;
                expectedPos = new BitSet(positions.Count);
                for (int i = context.CurrentState.NumberOfRunningPos - 1; i >=0; i--) { 
                    Debug.Assert(runningPositions[i].curpos != null);
                    expectedPos.Or(runningPositions[i].curpos);
                }
                for (int pos = expectedPos.NextSet(-1); pos != -1; pos = expectedPos.NextSet(pos)) {
                    int symbol = positions[pos].symbol;
                    if (symbol >= 0) { //non range nodes
                        XmlSchemaParticle p = positions[pos].particle as XmlSchemaParticle;
                        if (p == null) {
                           continue;
                        }
                        AddParticleToExpected(p, schemaSet, particles);
                    }
                }
            }
            return particles;
        }
    }

    sealed class AllElementsContentValidator : ContentValidator {
        Hashtable elements;     // unique terminal names to positions in Bitset mapping
        object[] particles;
        BitSet isRequired;      // required flags
        int countRequired = 0;

        public AllElementsContentValidator(XmlSchemaContentType contentType, int size, bool isEmptiable) : base(contentType, false, isEmptiable) {
            elements = new Hashtable(size);
            particles = new object[size];
            isRequired = new BitSet(size);
        }

        public bool AddElement(XmlQualifiedName name, object particle, bool isEmptiable) {
            if (elements[name] != null) {
                return false;
            }
            int i = elements.Count;
            elements.Add(name, i);
            particles[i] = particle;
            if (!isEmptiable) {
                isRequired.Set(i);
                countRequired ++;
            }
            return true;
        }

        public override bool IsEmptiable { 
            get { return base.IsEmptiable || countRequired == 0; }
        }

        public override void InitValidation(ValidationState context) {
            Debug.Assert(elements.Count > 0);
            context.AllElementsSet = new BitSet(elements.Count); //
            context.CurrentState.AllElementsRequired = -1; // no elements at all
        }

        public override object ValidateElement(XmlQualifiedName name, ValidationState context, out int errorCode) {
            object lookup = elements[name];
            errorCode = 0;
            if (lookup == null) {
                context.NeedValidateChildren = false;
                return null;
            }
            int index = (int)lookup;
            if (context.AllElementsSet[index]) {
                errorCode = -2;
                return null;
            }
            if (context.CurrentState.AllElementsRequired == -1) {
                context.CurrentState.AllElementsRequired = 0;
            }
            context.AllElementsSet.Set(index);
            if (isRequired[index]) {
                context.CurrentState.AllElementsRequired++;
            }
            return particles[index];
        }
 
        public override bool CompleteValidation(ValidationState context) {
            if (context.CurrentState.AllElementsRequired == countRequired || IsEmptiable && context.CurrentState.AllElementsRequired == -1) {
                return true;
            }
            return false;
        }

        public override ArrayList ExpectedElements(ValidationState context, bool isRequiredOnly) {
            ArrayList names = null;
            foreach (DictionaryEntry entry in elements) {
                if (!context.AllElementsSet[(int)entry.Value] && (!isRequiredOnly || isRequired[(int)entry.Value])) {
                    if (names == null) {
                        names = new ArrayList();
                    }
                    names.Add(entry.Key);
                }
            }
            return names;
        }

        public override ArrayList ExpectedParticles(ValidationState context, bool isRequiredOnly, XmlSchemaSet schemaSet) {
            ArrayList expectedParticles = new ArrayList();
            foreach (DictionaryEntry entry in elements) {
                if (!context.AllElementsSet[(int)entry.Value] && (!isRequiredOnly || isRequired[(int)entry.Value])) {
                    AddParticleToExpected(this.particles[(int)entry.Value] as XmlSchemaParticle, schemaSet, expectedParticles);
                }
            }
            return expectedParticles;
        }
    }
    #endregion
}
