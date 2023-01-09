using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BoardUI
{
    private float topLeftX;
    private float bottomLeftY;
    private float w = 16f;
    private float h = 8f;
    // controls how high the board is, lower value -> higher board
    private float trueH = 9.5f;
    private float tileWidth;
    public int searchDepth;
    public bool transpositionTableEnabled;

    private bool held = false;
    private GameObject overlay;
    private int heldTile;
    private Tile heldTileObject;
    public Tile.Player currentPlayer = Tile.Player.Blue;
    private Tile.Player winning = Tile.Player.Neutral;
    public int movesLeft;

    public int[] blueTiles;
    public int[] redTiles;
    public Tile[,] tiles;
    public bool randomFirstMove;

    public static int width;
    public static int height;

    private Color red = new Color(0.53f, 0.13f, 0.13f);
    private Color blue = new Color(0.13f, 0.13f, 0.53f);
    private Color gray = new Color(0.63f, 0.63f, 0.63f);
    private Color darkGray = new Color(0.13f, 0.13f, 0.13f, 0f);
    private Color highlighted = new Color(0.96f, 0.93f, 0.53f, 0.5f);

    private Color lightRed = new Color(1f, 0.4f, 0.4f);
    private Color lightBlue = new Color(0.4f, 0.4f, 1f);

    private GameObject tilePrefab;
    private GameObject circlePrefab;
    private TMP_Text movesLeftText;
    private TMP_Text playerWinsText;
    private TMP_Text newGameText;
    private int moveLimit;
    private GameObject background;
    public Button newGameButton;
    public Board board;

    public Game.PlayerType bluePlayerType = Game.PlayerType.Human;
    public Game.PlayerType redPlayerType = Game.PlayerType.Human;
    private List<Tile> highlightedTiles;

    bool AIMoveInProgress = false;
    float AIMoveAnimationTime = 0.17f;
    float AIMoveAnimationProgress = 0f;
    private Move AIMove;

    public BoardUI(int width_, int height_, GameObject tilePrefab_, GameObject circlePrefab_, TMP_Text movesLeftText_, TMP_Text playerWinsText_, TMP_Text newGameText_, Button newButton_, int moveLimit_, bool adjustPositions=true)
    {
        width = width_;
        height = height_;
        tilePrefab = tilePrefab_;
        movesLeftText = movesLeftText_;
        playerWinsText = playerWinsText_;
        newGameText = newGameText_;
        circlePrefab = circlePrefab_;
        heldTileObject = new Tile(new float[3] { 0, 0, 0 }, tilePrefab, Tile.Player.Neutral, gray);
        newGameButton = newButton_;
        moveLimit = moveLimit_;
        highlightedTiles = new List<Tile>();

        float gameRatio = (float)width / (float)height;
        float aspectRatio = w / h;

        if (gameRatio > aspectRatio) {
            // wide
            h = w / gameRatio;
        } else {
            // tall
            w = gameRatio * h;
        }

        topLeftX = -w / 2;
        bottomLeftY = -h / 2 - (trueH-h)/2;

        // this number makes no sense but idk
        float topLeftY = bottomLeftY + h;
        float topSpace = (5 - topLeftY);
        float centerY = bottomLeftY + h / 2;

        TextUI.SetButtonScale(newGameButton, w / 2.5f, w / 8f);

        movesLeft = moveLimit_;
        playerWinsText.fontSize = w * 21f;
        newGameText.fontSize = w * 9f;
        movesLeftText.fontSize = 80f * topSpace;
        UpdateMoveCounter();

        background = UnityEngine.Object.Instantiate(tilePrefab, new Vector2(0, (h-trueH)/2), Quaternion.identity);
        background.transform.localScale = new Vector2(w, h);
        background.GetComponent<SpriteRenderer>().sortingOrder = -1;

        if (adjustPositions)
        {
            // half a tile off from the bottom (add h/height * 1/2)
            float yc = bottomLeftY + h / (height * 2);
            TextUI.SetButtonY(newGameButton, yc);

            TextUI.SetTextY(movesLeftText, (topLeftY+topSpace/2));
            TextUI.SetTextY(playerWinsText, centerY);
        }

        tiles = new Tile[width, height];
        for (int i = 0; i < width; i++) {
            tiles[i, 0] = new Tile(PositionToCoord(i, 0), tilePrefab, Tile.Player.Blue, blue);
            tiles[i, height - 1] = new Tile(PositionToCoord(i, height - 1), tilePrefab, Tile.Player.Red, red);

            for (int j = 1; j < height - 1; j++) {
                tiles[i, j] = new Tile(PositionToCoord(i, j), tilePrefab, Tile.Player.Neutral, gray);
            }
        }
        board = new Board();
        board.Initialize(width, height);
        overlay = UnityEngine.Object.Instantiate(tilePrefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
        overlay.transform.localScale = new Vector2(w * 1.3f, 10f);
        overlay.GetComponent<SpriteRenderer>().sortingOrder = 1;
        SetOverlay();
    }

    public void Reset() {
        newGameButton.gameObject.SetActive(false);
        currentPlayer = Tile.Player.Blue;
        darkGray.a = 0;
        movesLeftText.color = new Color(1f, 1f, 1f);
        board.Initialize(width, height);
        UpdateTileDisplay(board.blueTiles, board.redTiles);
        playerWinsText.text = "";
        SetOverlay();
        winning = Tile.Player.Neutral;

        movesLeft = moveLimit;
        UpdateMoveCounter();
    }

    public void Destroy() {
        movesLeftText.color = new Color(1f, 1f, 1f);
        newGameButton.gameObject.SetActive(false);
        movesLeftText.text = "";
        playerWinsText.text = "";
        UnityEngine.Object.Destroy(overlay);
        UnityEngine.Object.Destroy(background);
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                tiles[i, j].Destroy();
            }
        }
    }

    private float[] PositionToCoord(int i, int j, float padding=0.075f) {
        // based on w, padding, and num, find t (tile width)
        // t*num+(t*padding)*(num+1)=w
        // t*num+t*padding*num+t*padding=w
        // t*(num+padding*num+padding) = w
        // t = w / (num+padding+num*padding)
        tileWidth = w / (width+padding+width*padding);
        float tileHeight = h / (height + padding + height * padding);
        float paddingWidth = tileWidth * padding;
        float x = (tileWidth + paddingWidth) * i + topLeftX + paddingWidth + tileWidth/2;
        float y = (tileHeight + paddingWidth) * j + bottomLeftY + paddingWidth + tileHeight/2;
        return new float[3] {x, y, tileWidth};
    }

    private int MousePositionToTile(Vector3 mousePosition) {
        // x and y between 0-1
        float x = (mousePosition.x - topLeftX) / w;
        float y = (mousePosition.y - bottomLeftY) / h;

        int tileX = (int)Math.Floor(x * width);
        int tileY = (int)Math.Floor(y * height);

        if ((0 <= tileX && tileX < width) && (0 <= tileY && tileY < height))
            return tileX * height + tileY;
        return -1;
    }

    private void SelectTile(int tile, Vector3 mousePosition, Tile.Player player) {
        if (heldTile != -1 && winning == Tile.Player.Neutral)
        {
            int tileX = tile / height;
            int tileY = tile % height;
            Tile t = tiles[tileX, tileY];
            if (t.player == player)
            {
                Color blendedColor = BlendColors(gray, highlighted);
                t.setColor(blendedColor);
                held = true;
                if (t.player == Tile.Player.Blue)
                    heldTileObject = new Tile(new float[3] { mousePosition.x, mousePosition.y, tileWidth }, tilePrefab, Tile.Player.Neutral, blue, 2);
                else
                    heldTileObject = new Tile(new float[3] { mousePosition.x, mousePosition.y, tileWidth }, tilePrefab, Tile.Player.Neutral, red, 2);
            }
        }
    }

    private void SelectTile(int tile, Tile.Player player)
    {
        if (heldTile != -1 && winning == Tile.Player.Neutral)
        {
            int tileX = tile / height;
            int tileY = tile % height;
            float[] position = PositionToCoord(tileX, tileY);
            Tile t = tiles[tileX, tileY];
            if (t.player == player)
            {
                t.setColor(gray);
                if (t.player == Tile.Player.Blue)
                    heldTileObject = new Tile(new float[3] { position[0], position[1], tileWidth }, tilePrefab, Tile.Player.Neutral, blue, 2);
                else
                    heldTileObject = new Tile(new float[3] { position[0], position[1], tileWidth }, tilePrefab, Tile.Player.Neutral, red, 2);
            }
        }
    }

    private void UpdateMoveAnimation() {
        AIMoveAnimationProgress += Time.deltaTime / AIMoveAnimationTime;
        if (AIMoveAnimationProgress >= 1)
        {
            PerformMoveOnBoard(AIMove, currentPlayer, false);
            AIMoveInProgress = false;
        }
        else {
            int startX = AIMove.StartSquare/ height;
            int startY = AIMove.StartSquare % height;
            int targetX = AIMove.TargetSquare / height;
            int targetY = AIMove.TargetSquare % height;

            float[] startPosition = PositionToCoord(startX, startY);
            float[] targetPosition = PositionToCoord(targetX, targetY);

            float a = AIMoveAnimationProgress;
            float interX = targetPosition[0] * a + startPosition[0] * (1 - a);
            float interY = targetPosition[1] * a + startPosition[1] * (1 - a);
            heldTileObject.setPosition(interX, interY);
        }
    }

    private bool IsMoveValid(Move move, Tile.Player player) {
        List<Move> validMoves = board.GetValidMoves(player);
        for (int i = 0; i < validMoves.Count; i++)
        {
            Move validMove = validMoves[i];
            if (move.StartSquare == validMove.StartSquare && move.TargetSquare == validMove.TargetSquare)
                return true;
        }
        return false;
    }

    public void PerformMoveOnBoard(Move move, Tile.Player player, bool calculateEval) {
        held = false;
        heldTileObject.Destroy();

        if (!IsMoveValid(move, player))
        {
            UpdateTileDisplay(board.blueTiles, board.redTiles);
            return;
        }

        board.MakeMove(move, player);
        UpdateTileDisplay(board.blueTiles, board.redTiles);
        SwitchPlayer();
        UpdateMoveCounter();

        winning = board.WinningPlayer(movesLeft);
        if (winning != Tile.Player.Neutral)
        {
            newGameButton.gameObject.SetActive(true);
            if (winning == Tile.Player.Blue)
            {
                playerWinsText.color = lightBlue;
                playerWinsText.text = "Blue wins!";
            }
            else
            {
                playerWinsText.color = lightRed;
                playerWinsText.text = "Red wins!";
            }
            movesLeftText.color = BlendColors(new Color(1f, 1f, 1f), darkGray);
        }
    }

    private void UpdateMoveCounter() {
        movesLeftText.text = movesLeft.ToString() + " Moves Left";
    }

    private void SetOverlay() {
        overlay.GetComponent<SpriteRenderer>().color = darkGray;
    }

    private Game.PlayerType CurrentPlayerType() {
        if (bluePlayerType == Game.PlayerType.Human && currentPlayer == Tile.Player.Blue)
            return Game.PlayerType.Human;
        if (redPlayerType == Game.PlayerType.Human && currentPlayer == Tile.Player.Red)
            return Game.PlayerType.Human;
        return Game.PlayerType.AI;
    }


    private void ReleaseHeldTile(int toTile, Tile.Player player) {
        Move move = new Move(heldTile, toTile);
        PerformMoveOnBoard(move, player, true);
    }

    private void DragHeldTileToMouse(Vector3 mousePosition) {
        heldTileObject.setPosition(mousePosition.x, mousePosition.y);
    }

    private void HighlightMove(Move move) {
        int i = move.TargetSquare / height;
        int j = move.TargetSquare % height;

        
        float[] tilePos = PositionToCoord(i, j);
        tilePos[2] /= 3.8f;
        Tile tile = new Tile(tilePos, circlePrefab, Tile.Player.Neutral, new Color(0f, 0f, 0f, 0.2f), 1);
        highlightedTiles.Add(tile);
    }

    private void DestroyHighlightedMoves() {
        for (int i = 0; i < highlightedTiles.Count; i++) {
            highlightedTiles[i].Destroy();
        }
        highlightedTiles.Clear();
    }

    public string EvalToString(float score, Tile.Player player) {
        int bonus = 0;
        if (score < -99)
        {
            if (player == Tile.Player.Blue)
            {
                bonus = -1;
            }
            else
            {
                bonus = 1;
            }
        }
        if (player == Tile.Player.Red)
            score *= -1;

        if (score > 99)
        {
            float roundedScore = (float)Math.Round(score);
            float blueBonus = (int)((score - roundedScore) * 100);
            int endMovesLeft = (int)(roundedScore - 100);

            int untilEnd = movesLeft - endMovesLeft - 1 + bonus;
            if (player == Tile.Player.Blue)
            {
                return "-M" + untilEnd.ToString() + "(" + blueBonus.ToString() + ")";
            }
            else {
                return "M" + untilEnd.ToString() + "(" + blueBonus.ToString() + ")";
            }
        }
        else if (score < -99)
        {
            score *= -1;
            float roundedScore = (float)Math.Round(score);
            float blueBonus = (int)(-(score - roundedScore) * 100);
            int endMovesLeft = (int)(roundedScore - 100);

            int untilEnd = movesLeft - endMovesLeft + bonus;
            if (player == Tile.Player.Red)
            {
                return "-M" + untilEnd.ToString() + "(" + blueBonus.ToString() + ")";
            }
            else
            {
                return "M" + untilEnd.ToString() + "(" + blueBonus.ToString() + ")";
            }
        }
        else
        {
            return score.ToString("0.000");
        }
    }

    public void Update() {
        if (movesLeft == searchDepth / 2 + 1)
        {
            searchDepth += 2;
        }
        // If the AI just spent a long time searching for the move time.deltatime is gonna be super high
        if (AIMoveInProgress && Time.deltaTime < 0.03f)
        {
            UpdateMoveAnimation();
        }
        else if (CurrentPlayerType() == Game.PlayerType.AI && winning == Tile.Player.Neutral && !AIMoveInProgress)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            // first move
            AI ai = new AI();
            ai.tt = new TranspositionTable();
            ai.tt.enabled = transpositionTableEnabled;
            float score;

            if (randomFirstMove && (movesLeft >= 12))
            {
                ai.nodes = 1;
                List<Move> validMoves = board.GetValidMoves(currentPlayer);
                int random = new System.Random().Next(0, validMoves.Count);
                ai.bestMove = validMoves[random];
                score = 0;
            }
            else
            {
                
                score = ai.Negamax(board, movesLeft, searchDepth, searchDepth, -Mathf.Infinity, Mathf.Infinity, currentPlayer);
            }
            stopwatch.Stop();

            float knps;
            if (stopwatch.ElapsedMilliseconds == 0)
                knps = 1;
            else
                knps = ai.nodes / stopwatch.ElapsedMilliseconds;
            string info = ", Depth: " + searchDepth + ", Nodes searched: " + ai.nodes.ToString() + ", NPS: " + knps.ToString() + "k";

            int bonus = 0;
            if (score < -99)
            {
                if (currentPlayer == Tile.Player.Blue)
                {
                    bonus = -1;
                }
                else
                {
                    bonus = 1;
                }
            }
            if (currentPlayer == Tile.Player.Red)
                score *= -1;

            if (score > 99)
            {
                int endMovesLeft = (int)Math.Round(score - 100);
                int untilEnd = movesLeft - endMovesLeft - 1 + bonus;
                if (untilEnd == 0)
                {
                    UnityEngine.Debug.Log("Blue wins");
                }
                else
                {
                    UnityEngine.Debug.Log("Eval: Blue M" + untilEnd.ToString() + info);
                }
            }
            else if (score < -99) {
                int endMovesLeft = (int)Math.Round(-score - 100);
                int untilEnd = movesLeft - endMovesLeft + bonus;
                if (untilEnd == 0) {
                    UnityEngine.Debug.Log("Red wins");
                } else {
                    UnityEngine.Debug.Log("Eval: Red M" + untilEnd.ToString() + info);
                }
            }
            else
            {
                UnityEngine.Debug.Log("Eval (Blue): " + score.ToString("0.000") + info);
            }
            AIMoveInProgress = true;
            AIMoveAnimationProgress = 0f;

            AIMove = ai.bestMove;
            SelectTile(AIMove.StartSquare, currentPlayer);
            UpdateMoveCounter();
        }

        if (winning != Tile.Player.Neutral)
        {
            if (darkGray.a < 0.85f)
            {
                darkGray.a += Time.deltaTime * 4f;
            }
            SetOverlay();
            movesLeftText.color = BlendColors(Color.white, darkGray);
        }
        else if (Input.GetMouseButton(0) && !AIMoveInProgress)
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (!held)
            {
                // select
                heldTile = MousePositionToTile(mousePosition);
                List<Move> validMoves = board.GetValidMoves(currentPlayer);
                for (int i = 0; i < validMoves.Count; i++) {
                    Move move = validMoves[i];
                    if (move.StartSquare == heldTile)
                        HighlightMove(move);
                }
                SelectTile(heldTile, mousePosition, currentPlayer);
            }
            DragHeldTileToMouse(mousePosition);            
        }
        else if (held && !AIMoveInProgress)
        {
            // release
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            DestroyHighlightedMoves();
            int toTile = MousePositionToTile(mousePosition);
            ReleaseHeldTile(toTile, currentPlayer);
            UpdateMoveCounter();
        }
    }

    private void SwitchPlayer() {
        if (currentPlayer == Tile.Player.Blue)
        {
            currentPlayer = Tile.Player.Red;
            movesLeft -= 1;
        }
        else {
            currentPlayer = Tile.Player.Blue;
        }
    }

    public PieceList[] GetPieceLists() {
        PieceList blueTiles = new PieceList(width);
        PieceList redTiles = new PieceList(width);
        PieceList[] allTiles = new PieceList[] { blueTiles, redTiles };

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (tiles[i, j].player == Tile.Player.Blue)
                {
                    blueTiles.AddPieceAtSquare(i * height + j);
                }
                else if (tiles[i, j].player == Tile.Player.Red)
                {
                    redTiles.AddPieceAtSquare(i * height + j);
                }
            }
        }

        return allTiles;
    }

    public void UpdateTileDisplay(PieceList blueTiles, PieceList redTiles) {
        ResetTiles();

        for (int i = 0; i < blueTiles.Count; i++) {
            int t = blueTiles[i];
            int x = t / height;
            int y = t % height;
            tiles[x, y].player = Tile.Player.Blue;
            tiles[x, y].setColor(blue);
        }
        for (int i = 0; i < redTiles.Count; i++)
        {
            int t = redTiles[i];
            int x = t / height;
            int y = t % height;
            tiles[x, y].player = Tile.Player.Red;
            tiles[x, y].setColor(red);
        }
    }

    public void ResetTiles() {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                tiles[i, j].player = Tile.Player.Neutral;
                tiles[i, j].setColor(gray);
            }
        }
    }

    private Color BlendColors(Color c1, Color c2)
    {
        float r = c1.r * (1 - c2.a) + c2.r * c2.a;
        float g = c1.g * (1 - c2.a) + c2.g * c2.a;
        float b = c1.b * (1 - c2.a) + c2.b * c2.a;
        return new Color(r, g, b);
    }
}