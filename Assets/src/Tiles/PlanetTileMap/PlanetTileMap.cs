﻿//using Entitas;
using System;
using System.Runtime.CompilerServices;
using TileProperties;
using Enums;

namespace PlanetTileMap
{
    //public struct PlanetMap : IComponent
    public struct PlanetTileMap
    {
        // public static const PlanetTile AirTile = new PlanetTile(); - PlanetTile cannot be const in c#?
        public static readonly PlanetTile AirTile = new PlanetTile();

        public int Xsize;
        public int Ysize;

        public int XChunkSize; // Size in chunks
        public int YChunkSize;

        public PlanetTileMapChunk[] ChunkList;
        public int[] ChunkIndexList; // 0 = error, 1 = empty, 2 = unexplored (TODO)

        private int NextChunk;

        private static PlanetTileMapChunk ErrorChunk = new PlanetTileMapChunk(); // todo: fill this with error tiles
        private static PlanetTileMapChunk EmptyChunk = new PlanetTileMapChunk();

        public PlanetWrapBehavior WrapBehavior;

        public PlanetTileMap(int xsize, int ysize) : this()
        {
            Xsize = xsize;
            Ysize = ysize;

            XChunkSize = Xsize >> 4;
            YChunkSize = Ysize >> 4;

            WrapBehavior = PlanetWrapBehavior.NoWrapAround; // Set to WrapAround manually if needed

            NextChunk = 0;

            ChunkIndexList = new int[XChunkSize * YChunkSize];
            ChunkList = new PlanetTileMapChunk[XChunkSize * YChunkSize];

            for (int i = 0; i < ChunkIndexList.Length; i++)
                ChunkIndexList[i] = 2;
        }

        // Is this really the only way to inline a function in c#?
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetChunkIndex(int x, int y)
        {
            if (WrapBehavior == PlanetWrapBehavior.WrapAround) x %= Xsize;

            return ChunkIndexList[(x >> 4) * YChunkSize + (y >> 4)];
        }

        private int AddChunk(PlanetTileMapChunk chunk, int x, int y)
        {
            // I feel like resizing by 1 each time is not very efficient... Change it later?
            Array.Resize(ref ChunkList, NextChunk + 1);

            if (WrapBehavior == PlanetWrapBehavior.WrapAround) x %= Xsize;
            chunk.ChunkIndexListID = (x >> 4) * YChunkSize + (y >> 4);

            ChunkIndexList[chunk.ChunkIndexListID] = NextChunk + 3;
            ChunkList[NextChunk] = chunk;
            NextChunk++;
            return NextChunk + 2;
        }

        public PlanetTileMapChunk GetChunk(int x, int y)
        {
            int ChunkIndex = GetChunkIndex(x, y);
            switch (ChunkIndex)
            {
                case 0: return ErrorChunk;
                case 1: return EmptyChunk;
                case 2: return EmptyChunk; // UNEXPLORED
            }

            ChunkList[ChunkIndex - 3].Usage++;
            return ChunkList[ChunkIndex - 3];
        }

        public ref PlanetTileMapChunk GetChunkRef(int x, int y)
        {
            int ChunkIndex = GetChunkIndex(x, y);

            if (ChunkIndex == 0) throw new IndexOutOfRangeException();
            if (ChunkIndex < 3)
                // We are getting a reference here, most likely to edit the chunk / add a tile, so we can't just return an empty chunk
                // Instead, we will just create a new chunk
                ChunkIndex = AddChunk(new PlanetTileMapChunk(), x, y);

            ChunkList[ChunkIndex - 3].Usage++;
            return ref ChunkList[ChunkIndex - 3];
        }

        public void SetChunk(int x, int y, PlanetTile[,] tiles)
        {
            int ChunkIndex = GetChunkIndex(x, y);
            if (ChunkIndex == 0) return;
            if (ChunkIndex < 3) ChunkIndex = AddChunk(new PlanetTileMapChunk(), x, y);

            ChunkList[ChunkIndex - 3].Seq++;

            for (int i = 0; i < 16; i++)
                for (int j = 0; j < 16; j++)
                    ChunkList[ChunkIndex - 3].Tiles[i, j] = tiles[i, j];
        }
        public ref PlanetTile GetTileRef(int x, int y)
        {
            ref PlanetTileMapChunk chunk = ref GetChunkRef(x, y);

            chunk.Seq++; // We are getting a reference to the tile, so we are probably modifying the tile, hence increment seq

            return ref chunk.Tiles[x & 0x0F, y & 0x0F];
        }

        public PlanetTile GetTile(int x, int y)
        {
            int ChunkIndex = GetChunkIndex(x, y);
            if (ChunkIndex == 1) return AirTile;

            return ChunkList[ChunkIndex - 3].Tiles[x & 0x0F, y & 0x0f];
        }

        public void SetTile(int x, int y, PlanetTile tile)
        {
            ref PlanetTileMapChunk chunk = ref GetChunkRef(x, y);
            chunk.Seq++; // Updating tile, increment seq
            chunk.Tiles[x & 0x0F, y & 0x0F] = tile;
        }

        public ref PlanetTile getTile(int x, int y)
        {
            ref PlanetTileMapChunk chunk = ref GetChunkRef(x, y);
            return ref chunk.Tiles[x & 0x0F, y & 0x0F];
        }

        // Sort chunks by most used using quick sort

        private void swap(int index1, int index2)
        {
            PlanetTileMapChunk tmpchunk = ChunkList[index1];

            // Swap chunks
            ChunkList[index1] = ChunkList[index2];
            ChunkList[index2] = tmpchunk;

            // Then update chunk index list - This is what storing the Position inside the chunk is most useful for
            ChunkIndexList[ChunkList[index1].ChunkIndexListID] = index1 + 3;
            ChunkIndexList[ChunkList[index2].ChunkIndexListID] = index2 + 3;
        }

        private int partition(int start, int end)
        {
            // Use negative of the usage to have the list sorted from most used to least used without having to reverse afterwards
            int p = -ChunkList[start].Usage;

            int count = 0;
            for (int k = start + 1; k <= end; k++)
                if (-ChunkList[k].Usage <= p)
                    count++;

            int pi = start + count;
            swap(pi, start);

            int i = start, j = end;

            while (i < pi && j > pi)
            {
                while (-ChunkList[i].Usage <= p) i++;
                while (-ChunkList[j].Usage > p) j--;

                if (i < pi && j > pi)
                    swap(i++, j--);
            }

            return pi;
        }

        private void quickSort(int start, int end)
        {
            if (start >= end) return;

            int p = partition(start, end);
            quickSort(start, p - 1);
            quickSort(p + 1, end);
        }

        public void SortChunks()
        {
            // Sort chunks from most used to least used
            if (ChunkList == null || ChunkList.Length == 0) return;

            quickSort(0, NextChunk - 1);
        }
    

        //Take in PlanetTileMap, and map a horizonal line
    public void GenerateFlatPlanet()
        {
            //default size = X...

            //make a single line horizonally across planet
            //from left to right

            //int TileId = GetTileId("default-tile")
           //for x = 0 to x = Planet.Size.X
           //Planet.SetTile(TileId, x, 10)


        }
    }
}
