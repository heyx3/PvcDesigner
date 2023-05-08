using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Assert = UnityEngine.Assertions.Assert;

namespace PVC
{
    public class Graph : MonoBehaviour
    {
        public float MarginOfError = 0.0002f; // 1/5 mm


        public IReadOnlyDictionary<Piece, IReadOnlyCollection<Piece>> Connections
            => connectionsInterface;
        public IReadOnlyDictionary<(Piece, AttachmentPoint), (Piece, AttachmentPoint)> Attachments
            => attachments;

        public IReadOnlyDictionary<Transform, ICollection<Piece>> IslandPieces
            => islandPiecesInterface;
        public IReadOnlyDictionary<Piece, Transform> IslandParents
            => islandParents;

        public IEnumerable<Piece> AllPieces
            => connectionsInterface.Keys;


        #region Private fields for the above properties

        //Underlying collections:
        private Dictionary<Piece, HashSet<Piece>> connections = new Dictionary<Piece, HashSet<Piece>>();
        private Dictionary<(Piece, AttachmentPoint), (Piece, AttachmentPoint)> attachments = new Dictionary<(Piece, AttachmentPoint), (Piece, AttachmentPoint)>();
        private Dictionary<Transform, HashSet<Piece>> islandPieces = new Dictionary<Transform, HashSet<Piece>>();
        private Dictionary<Piece, Transform> islandParents = new Dictionary<Piece, Transform>();

        //Read-only facades (needed for nested collections):
        private Utils.CastedReadOnlyDictionary<Piece,
                                               HashSet<Piece>, IReadOnlyCollection<Piece>,
                                               Dictionary<Piece, HashSet<Piece>>
                                              > connectionsInterface;
        private Utils.CastedReadOnlyDictionary<Transform,
                                               HashSet<Piece>, ICollection<Piece>,
                                               Dictionary<Transform, HashSet<Piece>>
                                              > islandPiecesInterface;

        #endregion

        private uint nextIslandID = 1;
        private Transform CreateIsland()
        {
            string name = $"Island {nextIslandID++}";
            Transform newIsland = new GameObject(name).transform;

            islandPieces.Add(newIsland, new HashSet<Piece>() { });

            return newIsland;
        }


        public void AddPiece(Piece piece)
        {
            Assert.IsFalse(piece.Points.Any(p => attachments.ContainsKey((piece, p))),
                           "A new piece's attachment points are already in use");

            connections.Add(piece, new HashSet<Piece>());

            //Create a new island containing this piece.
            var newIsland = CreateIsland();
            islandPieces[newIsland].Add(piece);
            islandParents.Add(piece, newIsland);

            //Find what this piece is attached to, based on its physical position.
            UpdateAttachments(piece);
        }
        public void RemovePiece(Piece piece, bool killObject = true)
        {
            bool check1 = connections.Remove(piece);
            Assert.IsTrue(check1, "Piece wasn't in 'connections'; was it even in the graph?");

            foreach (var point in piece.Points)
                attachments.Remove((piece, point));

            //Remove this piece from its island.
            var islandParent = islandParents[piece];
            islandParents.Remove(piece);
            bool check3 = islandPieces[islandParent].Remove(piece);
            Assert.IsTrue(check3, "Piece wasn't in its island");
            RegenIsland(islandParent);

            if (killObject)
                Destroy(piece.gameObject);
        }

        /// <summary>
        /// Calculates the current attachments for a piece based on its position.
        /// Returns whether any attachments have been changed (new ones found, old ones detached).
        /// </summary>
        public bool UpdateAttachments(Piece piece)
        {
            bool anythingChanged = false;
            float marginOfErrorSqr = MarginOfError * MarginOfError;
            foreach (var attachment in piece.Points)
            {
                var aPos = attachment.transform.position;

                //Disconnect the existing attachment if it moved away.
                if (attachments.TryGetValue((piece, attachment), out var a2))
                {
                    var (piece2, attachment2) = a2;
                    if ((aPos - attachment2.transform.position).sqrMagnitude >= marginOfErrorSqr)
                    {
                        anythingChanged = true;

                        bool check1 = connections[piece].Remove(piece2),
                             check2 = connections[piece2].Remove(piece);
                        Assert.IsTrue(check1 && check2, "'connections' doesn't line up with 'attachments'");

                        attachments.Remove((piece, attachment));
                        attachments.Remove((piece2, attachment2));

                        Assert.IsTrue(islandParents[piece] == islandParents[piece2],
                                      "Connected pieces were on different islands");
                        RegenIsland(islandParents[piece]);
                    }    
                }
                //Connect to a new attachment if it moved over here.
                else
                {
                    foreach (var otherPiece in AllPieces)
                        if (otherPiece != piece)
                            foreach (var otherAttachment in otherPiece.Points)
                                if (!attachments.ContainsKey((otherPiece, otherAttachment)))
                                    if ((aPos - otherAttachment.transform.position).sqrMagnitude < marginOfErrorSqr)
                                    {
                                        anythingChanged = true;

                                        //Record the specific attachment points.
                                        attachments.Add((piece, attachment),
                                                        (otherPiece, otherAttachment));
                                        attachments.Add((otherPiece, otherAttachment),
                                                        (piece, attachment));

                                        //If the pieces are on separate islands, merge them.
                                        var islandParent1 = islandParents[piece];
                                        var islandParent2 = islandParents[otherPiece];
                                        if (islandParent1 != islandParent2)
                                        {
                                            foreach (var otherIslandPiece in islandPieces[islandParent2])
                                                islandParents[otherIslandPiece] = islandParent1;
                                            islandPieces.Remove(islandParent2);
                                            Destroy(islandParent2.gameObject);
                                        }
                                    }
                }
            }
            return anythingChanged;
        }

        //TODO: Detatch()
        //TODO: InsertPipe()
        //TODO: SplitPipe()


        /// <summary>
        /// Checks a recently-modified island for a schism,
        ///     potentially splitting it into multiple islands.
        /// </summary>
        private void RegenIsland(Transform parent)
        {
            var newIslands = new List<HashSet<Piece>>();
            var newIslandsPerPiece = new Dictionary<Piece, int>();
            foreach (var piece in islandPieces[parent])
            {
                //Has this piece already been found in a flood-fill?
                if (newIslandsPerPiece.ContainsKey(piece))
                    continue;

                //Otherwise, this is part of a new island.
                var newIslandPieces = new HashSet<Piece>();
                newIslands.Add(newIslandPieces);
                int newIslandIdx = newIslands.Count - 1;

                //Grab all connected pieces with a flood-fill algorithm.
                var searchFrontier = new Queue<Piece>();
                searchFrontier.Enqueue(piece);
                while (searchFrontier.Count > 0)
                {
                    var nextPiece = searchFrontier.Dequeue();
                    if (newIslandsPerPiece.ContainsKey(nextPiece))
                        continue;

                    newIslandsPerPiece.Add(nextPiece, newIslandIdx);
                    foreach (var nextNextPiece in connections[nextPiece])
                        searchFrontier.Enqueue(nextNextPiece);
                }
            }

            //If there are no pieces in the island, destroy it.
            if (newIslands.Count < 1)
            {
                islandPieces.Remove(parent);
                Destroy(parent.gameObject);
                return;
            }
            //If there is still only one island, don't change anything.
            else if (newIslands.Count == 1)
            {
                return;
            }

            //Otherwise, use the existing island for the first group of pieces.
            islandPieces[parent] = newIslands[0];
            //Create new island objects for the other groups.
            var newIslandParents = newIslands.Skip(1).Select(i => CreateIsland()).ToList();

            //Update the piece-to-island mapping for each piece of the old island.
            foreach (var kvp in newIslandsPerPiece)
                islandParents[kvp.Key] = newIslandParents[kvp.Value];
        }


        private void Awake()
        {
            //Initialize the read-only facades.
            connectionsInterface = new Utils.CastedReadOnlyDictionary<Piece, HashSet<Piece>, IReadOnlyCollection<Piece>, Dictionary<Piece, HashSet<Piece>>>(
                connections
            );
            islandPiecesInterface = new Utils.CastedReadOnlyDictionary<Transform, HashSet<Piece>, ICollection<Piece>, Dictionary<Transform, HashSet<Piece>>>(
                islandPieces
            );
        }
    }
}
