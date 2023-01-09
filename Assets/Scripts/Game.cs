using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Game : MonoBehaviour
{
    public enum PlayerType { Human, AI };

    public int width = 5;
    public int height = 6;
    public int moveLimit = 13;
    public int searchDepth = 10;
    public bool transpositionTableEnabled = true;
    public PlayerType bluePlayerType = PlayerType.Human;
    public PlayerType redPlayerType = PlayerType.AI;
    public GameObject tilePrefab;
    public GameObject circlePrefab;
    public TMP_Text movesLeftText;
    public TMP_Text playerWinsText;
    public TMP_Text newGameText;
    public Button newButton;

    public TMP_Text PlayVSHumanText;
    public TMP_Text PlayVSComputerText;
    public TMP_Text PlayAsBlueText;
    public TMP_Text PlayAsRedText;
    public TMP_Text LevelText;

    public Button PlayVSHumanButton;
    public Button PlayVSComputerButton;
    public Button PlayAsBlueButton;
    public Button PlayAsRedButton;
    public Slider LevelSlider;

    public static BoardUI board;

    const float w = 16f;
    const float h = 10f;
    bool first_init = false;
    bool gameInProgress = false;
    bool inAISelectionMenu = false;
    public bool randomFirstMove = true;

    public bool AIAnalysisDebug = false;
    private int moveIndex = 0;
    private AIGame[] games;

    // Start is called before the first frame update
    void Start()
    {
        // update resolution to position UI correctly
        LevelText.text = "";
        newButton.onClick.AddListener(newGame);
        newButton.gameObject.SetActive(false);
        PlayAsBlueButton.gameObject.SetActive(false);
        PlayAsRedButton.gameObject.SetActive(false);
        LevelSlider.gameObject.SetActive(false);

        float frac = 7;
        float dif = h * 2.8f / (frac*2);
        TextUI.SetButtonScale(PlayVSHumanButton, h * 4.6f/ frac, h / frac);
        PlayVSHumanText.fontSize = (h/frac) * 80f;
        TextUI.SetButtonY(PlayVSHumanButton, dif/2);
        PlayVSHumanButton.onClick.AddListener(InitHumanVsHuman);

        TextUI.SetButtonScale(PlayVSComputerButton, h * 4.6f / frac, h / frac);
        PlayVSComputerText.fontSize = (h / frac) * 80f;
        TextUI.SetButtonY(PlayVSComputerButton, -dif/2);
        PlayVSComputerButton.onClick.AddListener(AISelectionMenu);

        TextUI.SetButtonScale(PlayAsBlueButton, h * 4.6f / frac, h / frac);
        PlayAsBlueText.fontSize = (h / frac) * 80f;
        PlayAsBlueButton.onClick.AddListener(InitPlayAsBlue);

        TextUI.SetButtonScale(PlayAsRedButton, h * 4.6f / frac, h / frac);
        TextUI.SetButtonY(PlayAsRedButton, -dif);
        PlayAsRedText.fontSize = (h / frac) * 80f;
        PlayAsRedButton.onClick.AddListener(InitPlayAsRed);

        TextUI.SetSliderY(LevelSlider, dif);
        TextUI.SetSliderScale(LevelSlider, h * 4.6f / frac, h / frac);

        TextUI.SetTextY(LevelText, dif * 1.5f);
        LevelText.fontSize = (h / frac) * 62f;
    }

    private int GetSearchDepth() {
        float value = LevelSlider.value * 2 - 2;
        if (value == 0)
            value = 1;
        return (int)value;
    }

    private void InitHumanVsHuman() {
        moveIndex = 0;
        bluePlayerType = PlayerType.Human;
        redPlayerType = PlayerType.Human;

        if (AIAnalysisDebug)
        {
            bluePlayerType = PlayerType.AI;
            redPlayerType = PlayerType.AI;
        }

        InitBoard(searchDepth, !first_init);
        first_init = true;

        /*
        if (AIAnalysisDebug) {
            AiAnalysis aiAnalysis = new AiAnalysis();
            games = aiAnalysis.Games(2500, 8, 8);

            int[] bluePositionWinrate = new int[30];
            int[] redPositionWinrate = new int[30];
            int[] bluePositionLoserate = new int[30];
            int[] redPositionLoserate = new int[30];
            for (int i = 0; i < games.Length; i++) {
                AIGame game = games[i];
                List<Move> moveHistory = game.MoveHistory;
                bool blueWon = game.Outcome == Tile.Player.Blue;
                bool isBlue = true;
                for (int j = 0; j < moveHistory.Count; j++) {
                    int target = moveHistory[j].TargetSquare;
                    if (isBlue && blueWon)
                    {
                        bluePositionWinrate[target] += 1;
                    }
                    else if (isBlue)
                    {
                        bluePositionLoserate[target] += 1;
                    }
                    else if (!blueWon)
                    {
                        redPositionWinrate[target] += 1;
                    }
                    else {
                        redPositionLoserate[target] += 1;
                    }
                    isBlue = !isBlue;
                }
            }

            float[] bluePositionWR = new float[30];
            for (int i = 0; i < 30; i++) {
                float w = (float)bluePositionWinrate[i];
                float l = (float)bluePositionLoserate[i];
                float wr = (w+1) / (w +l+2);
                bluePositionWR[i] = wr;
            }
            board.board.DebugTable(bluePositionWR);
            AiAnalysis.DebugArray(bluePositionWR);

            float[] redPositionWR = new float[30];
            for (int i = 0; i < 30; i++)
            {
                float w = (float)redPositionWinrate[i];
                float l = (float)redPositionLoserate[i];
                float wr = (w + 1) / (w + l + 2);
                redPositionWR[i] = wr;
            }
            board.board.DebugTable(redPositionWR);
            AiAnalysis.DebugArray(redPositionWR);
            // Debug.Log(game.Outcome);
            // InvokeRepeating("NextMove", 1, 1);
        }
        */
    }

    private void NextMove() {
        Move move = games[0].MoveHistory[moveIndex];
        board.PerformMoveOnBoard(move, board.currentPlayer, false);
        moveIndex += 1;
    }

    private void InitPlayAsBlue()
    {
        PlayAsBlueButton.gameObject.SetActive(false);
        PlayAsRedButton.gameObject.SetActive(false);
        LevelSlider.gameObject.SetActive(false);
        bluePlayerType = PlayerType.Human;
        redPlayerType = PlayerType.AI;
        float value = LevelSlider.value * 2 - 2;
        if (value == 0)
            value = 1;
        InitBoard((int)value, !first_init);
        first_init = true;
    }

    private void InitPlayAsRed()
    {
        PlayAsBlueButton.gameObject.SetActive(false);
        PlayAsRedButton.gameObject.SetActive(false);
        LevelSlider.gameObject.SetActive(false);
        bluePlayerType = PlayerType.AI;
        redPlayerType = PlayerType.Human;
        float value = LevelSlider.value * 2 - 2;
        if (value == 0)
            value = 1;
        InitBoard((int)value, !first_init);
        first_init = true;
    }

    private void AISelectionMenu() {
        PlayVSHumanButton.gameObject.SetActive(false);
        PlayVSComputerButton.gameObject.SetActive(false);
        PlayAsBlueButton.gameObject.SetActive(true);
        PlayAsRedButton.gameObject.SetActive(true);
        LevelSlider.gameObject.SetActive(true);
        inAISelectionMenu = true;
    }

    private void InitBoard(int searchDepth_, bool t=true) {
        gameInProgress = true;
        PlayVSHumanButton.gameObject.SetActive(false);
        PlayVSComputerButton.gameObject.SetActive(false);
        board = new BoardUI(width, height, tilePrefab, circlePrefab, movesLeftText, playerWinsText, newGameText, newButton, moveLimit, t);
        board.searchDepth = searchDepth_;
        board.bluePlayerType = bluePlayerType;
        board.redPlayerType = redPlayerType;
        board.transpositionTableEnabled = transpositionTableEnabled;
        board.randomFirstMove = randomFirstMove;
        inAISelectionMenu = false;
    }

    private void newGame()
    {
        board.Destroy();
        gameInProgress = false;
        PlayVSHumanButton.gameObject.SetActive(true);
        PlayVSComputerButton.gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (inAISelectionMenu)
            LevelText.text = "Level: " + LevelSlider.value.ToString();
        else
            LevelText.text = "";
        if (Input.GetKeyDown(KeyCode.A)) {
            DebugAnalysis();
        }
        if (gameInProgress)
            board.Update();
    }

    void DebugAnalysis() {
        List<Move> validMoves = board.board.GetValidMoves(board.currentPlayer);
        float[] scores = new float[validMoves.Count];
        Tile.Player player = Tile.Player.Blue;
        if (board.currentPlayer == Tile.Player.Blue)
        {
            player = Tile.Player.Red;
        }

        AI ai = new AI();
        ai.tt = new TranspositionTable();
        ai.tt.enabled = true;
        for (int i = 0; i < validMoves.Count; i++) {
            Move move = validMoves[i];
            board.board.MakeMove(move, board.currentPlayer);
            if (board.currentPlayer == Tile.Player.Blue)
            {
                board.movesLeft -= 1;
            }
            float score = ai.Negamax(board.board, board.movesLeft, searchDepth-1, searchDepth-1, -Mathf.Infinity, Mathf.Infinity, player);
            scores[i] = score*-1;
            if (board.currentPlayer == Tile.Player.Blue)
                board.movesLeft += 1;
            board.board.UnmakeMove(move, board.currentPlayer);
        }

        MoveOrdering moveOrdering = new MoveOrdering();
        moveOrdering.moveScores = scores;
        moveOrdering.Sort(validMoves);

        string msg = "";
        for (int i = 0; i < validMoves.Count; i++) {
            Move move = validMoves[i];
            float score = scores[i]*-1;
            msg += MoveToString(move);
            msg += ": " + board.EvalToString(score, player);
            if (i < validMoves.Count - 1)
                msg += " | ";
        }
        Debug.Log(msg);
    }

    string MoveToString(Move move) {
        int start = move.StartSquare;
        int to = move.TargetSquare;

        int xStart = start / height;
        int xTo = to / height;
        int yStart = start % height + 1;
        int yTo = to % height + 1;

        string first = NumToAlpha(xStart) + yStart.ToString();
        string second = NumToAlpha(xTo) + yTo.ToString();

        return first + "-" + second;
    }

    char NumToAlpha(int a) {
        string alpha = "ABCDEGFHIJKLMNOPQRSTUVWXYZ";
        return alpha[a];
    }
}
