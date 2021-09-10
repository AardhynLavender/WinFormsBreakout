﻿
//
//  BreakOutGame Class
//
//  Defines the functionality and members to create and play Atari Breakout
//  with score counters, powerups, levels, and saving.
//  
//  BreakoutGame is responsible for managing the physics of its game objects
//  and the initalization and freeing of them.
//

using Breakout.GameObjects;
using Breakout.Render;
using Breakout.Utility;
using Breakout.Utility.levels;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;

namespace Breakout
{
    class BreakoutGame : Game
    {
        public const int TILE_SIZE          = 16;

        private const int LEVELS            = 3;
        private const int ROWS              = 6;
        private const int SCALE             = 3;

        private const int BALL_SPEED        = 5;
        private const int BALL_SIZE         = 6;

        private const int START_LIFES       = 3;
        private const int START_SCORE       = 0;
        private const int SCORE_LENGTH      = 6;

        private const int FONT_WIDTH        = 6;
        private const int FONT_HEIGHT       = 5;
        private const int HUD_MARGIN        = 10;

        // usefull tile coordiantes
        private const int CLOSE             = 26;
        private const int HEART             = 27;
        private const int POINT_TILE        = 30;

        private int score;
        private int lifes;
        private bool levelRunning;

        private MainMenu menu;

        private Level[] levels;
        private int currentLevel;

        private List<Ball> balls;
        private Ball ball => balls.First();

        private Paddle paddle;
        private GameObject backdrop;
        private GameObject closeButton;
        private Cursor cursor;

        private Text timeLabel;
        private Text gameTime;
        private Stopwatch gameStopwatch;

        private Text scoreLabel;
        private Text scoreDisplay;
        private Text livesLabel;
        private List<GameObject> lifeDisplay;

        private Augment currentAugment;

        private Animation[] heartbreak;

        public static readonly Tileset Tileset = 
            new Tileset(
                Properties.Resources.tileset, 
                Properties.Resources.tileset.Width, 
                TILE_SIZE, 
                TILE_SIZE
            );

        public static readonly Tileset Typeset =
            new Tileset(
                Properties.Resources.typeset,
                Properties.Resources.typeset.Width,
                FONT_WIDTH,
                FONT_HEIGHT
            );

        public static readonly Tileset Ballset =
            new Tileset(
                Properties.Resources.ball,
                Properties.Resources.ball.Width,
                BALL_SIZE,
                BALL_SIZE
            );

        // configuration fields

        private bool hasSfx;
        private bool hasCeiling;
        private bool hasLevels;
        private bool hasAugments;
        private bool hasInfiniteLives;
        private bool hasFloor;
        private bool hasPersistance;

        // properties

        public int Score 
        { 
            get => score;
            set
            {
                score = value;
                updateScore();
            }
        }

        public int Lives
        {
            get => lifes;
            set
            {
                lifes = value;
                updateLives();

                if (lifes == 0)
                {
                    EndGame();
                    lifes = START_LIFES;
                }
            }
        }

        public Level CurrentLevel
        {
            get => levels[currentLevel];
        }

        public bool LevelRunning        
        { 
            get => levelRunning;
            set => levelRunning = value; 
        }

        public int BallCount                    => balls.Count;
        public List<Ball> Balls                 => balls;
        public Ball Ball                        => balls.First();
        public Vector2D BallPosition            => new Vector2D(ball.X, ball.Y);

        public Paddle Paddle                    => paddle;

        public int Scale => SCALE;

        // configuration properties

        public bool HasSfx              { get => hasSfx; set => hasSfx = value; }
        public bool HasCeiling          { get => hasCeiling; set => hasCeiling = value; }
        public bool HasLevels           { get => hasLevels; set => hasLevels = value; }
        public bool HasAugments         { get => hasAugments; set => hasAugments = value; }
        public bool HasInfiniteLives    { get => hasInfiniteLives; set => hasInfiniteLives = value; }
        public bool HasFloor            { get => hasFloor; set => hasFloor = value; }
        public bool HasPersistance      { get => hasPersistance; set => hasPersistance = value; }

        // Constructor

        public BreakoutGame(Screen screen, SoundPlayer media, System.Windows.Forms.Timer ticker) 
            : base(screen, media, ticker)
        {
            // initalize fields

            screen.Scale    = SCALE;
            score           = START_SCORE;
            lifes           = START_LIFES;

            // provide game componants with reference to *this* class and the Screen

            GameComponant.BreakoutGame = this;
            GameComponant.Screen = Screen;
            GameComponant.Random = random;

            // initalize coordiante variables

            float x, y;

            // add backdrop

            x = -TILE_SIZE;
            y = 0 - Properties.Resources.levelBackdrop.Height + Screen.HeightPixels;
            backdrop = new GameObject(x, y, Properties.Resources.levelBackdrop, true);

            // create paddle

            paddle = new Paddle();

            // create ball

            balls = new List<Ball>();
            balls.Add(new Ball());

            // add score display

            scoreLabel      = new Text(HUD_MARGIN, HUD_MARGIN, "score");
            scoreDisplay    = new Text(HUD_MARGIN, HUD_MARGIN * 2);

            // add stopwatch display

            gameStopwatch = new Stopwatch();
            timeLabel = new Text(scoreLabel.Width + HUD_MARGIN * 3, HUD_MARGIN, "time"); ;
            gameTime = new Text(scoreLabel.Width + HUD_MARGIN * 3, HUD_MARGIN * 2);

            // add lives display

            lifeDisplay = new List<GameObject>(START_LIFES);

            livesLabel = new Text(0, HUD_MARGIN, "LIVES");
            livesLabel.X = screen.WidthPixels / 2 - livesLabel.Width / 2;

            x = screen.WidthPixels / 2 - START_LIFES * TILE_SIZE / 2;

            for (int i = 0; i < lifes; i++)
                lifeDisplay.Add(
                    new GameObject(
                        x + TILE_SIZE * i,
                        TILE_SIZE + 1,
                        Tileset.Texture,
                        Tileset.GetTile(HEART),
                        ghost: true
                    )
                );

            // add heart break animation to hearts

            heartbreak = new Animation[START_LIFES];

            for (int i = 0; i < START_LIFES; i++)
                heartbreak[i] = AddAnimation(
                    new Animation(
                        this,
                        lifeDisplay[i],
                        new List<Rectangle>()
                        {
                            Tileset.GetTile(HEART + 1),
                            Tileset.GetTile(HEART + 2)
                        },
                        Tileset,
                        Time.TWENTYTH_SECOND,
                        loop: false
                    )
                );


            // open main menu

            menu = (MainMenu)AddGameObject(new MainMenu());
            menu.Open();

            // create close button

            closeButton = AddGameObject(new GameObject(0, 2, Tileset.Texture, Tileset.GetTile(CLOSE), ghost: true));
            closeButton.X = Screen.WidthPixels - closeButton.Width;

            // create levels

            levels = new Level[LEVELS]
            {
                new Level(ROWS, Screen.WidthPixels, Tileset, 0, 8),
                new SecondLevel(ROWS, Screen.WidthPixels, Tileset, 0, 8),
                new SecondLevel(ROWS, Screen.WidthPixels, Tileset, 0, 8),
            };

            // create cursor

            cursor = (Cursor)AddGameObject(new Cursor());
        }
        
        protected override void Process()
        {
            base.Process();

            // paralax effect on backdrop
            backdrop.X = -TILE_SIZE / 2 - TILE_SIZE * (paddle.X + Paddle.Width / 2) / Screen.WidthPixels - 0.5f;

            // check if player pressed the close button
            if (Screen.MouseX / SCALE > closeButton.X
                && Screen.MouseX / SCALE < closeButton.X + closeButton.Width
                && Screen.MouseY / SCALE > closeButton.Y
                && Screen.MouseY / SCALE < closeButton.Y + closeButton.Height
                && Screen.MouseDown
                )
            {
                if (HasPersistance) SaveGame();
                Quit();
            }

            // process current augment if not null
            if (!(currentAugment is null))
            {
                // free augments if they go off the screen
                if (currentAugment.Y > screen.HeightPixels || !levelRunning)
                    ClearAugment();

                // hide augments that have been 'caught' off the screen
                else if (DoesCollide(paddle, currentAugment))
                    hideActiveAugment();
            }

            // level processing
            if (levelRunning)
            {
                // check if level has been cleared
                if (CurrentLevel.BrickCount <= 0)
                    NextLevel();

                updateTime();
            }
        }

        private void hideActiveAugment()
        {
            currentAugment.Velocity.Zero();
            currentAugment.X = currentAugment.Y = -20;
        }

        public void ClearAugment()
        {
            QueueFree(currentAugment);
            currentAugment = null;
        }

        protected override void Render()
            => base.Render();

        public void BrickHit(int index)
        {
            Brick brick = CurrentLevel.Bricks[index];
            brick.Hits++;

            // increment and show gained points
            Score += brick.Value * brick.Hits;
            floatPoints(brick);

            if (brick.HasBeenDestroyed)
            {
                // does this brick drop an augment
                if (currentAugment is null && HasAugments && CurrentLevel.DropAugment(out Augment augment, brick))
                    currentAugment = (Augment)AddGameObject(augment);

                brick.Explode();
            }
            else PlaySound(Properties.Resources.bounce);
        }

        private void NextLevel()
        {
            levelRunning = false;

            if (currentLevel + 1 < levels.Length && hasLevels)
            {
                // reject any active augments
                if (!(currentAugment is null)) currentAugment.Reject();

                // transition backdrop
                backdrop.Velocity.Y = 1.5f;

                // hide ball
                ball.Velocity.Zero();
                ball.X = ball.Y = -10;

                QueueTask(Time.SECOND * 2, () =>
                {
                    backdrop.Velocity.Zero();

                    // build the next level
                    currentLevel++;
                    levelRunning = true;
                    CurrentLevel.Build();

                    StartBall();
                });
            }
            else
            {
                // end the game
                EndGame();
            }
        }

        private void updateScore()
            => scoreDisplay.Value = Score.ToString($"D{SCORE_LENGTH}");

        private void updateLives()
        {
            if (lifes > -1) heartbreak[lifes].Animating = true;
        }

        private void updateTime()
        {
            gameTime.Value = $"{gameStopwatch.Elapsed.Minutes:D2} {gameStopwatch.Elapsed.Seconds:D2}";
        }

        public void StartBall()
        {
            // reset ball
            ball.X = Screen.WidthPixels / 2 - ball.Width / 2;

            // place ball as far up as possible (excluding level ceiling space)
            ball.Y = CurrentLevel.Ceiling + TILE_SIZE / 2;
            bool placedBall;
            do
            {
                placedBall = true;
                foreach (Brick brick in CurrentLevel.Bricks)
                    if (ball.Y < brick.Y + brick.Height)
                    {
                        ball.Y += TILE_SIZE;
                        placedBall = false;
                        break;
                    }
            }
            while (!placedBall);

            ball.Velocity.Zero();

            QueueTask(Time.SECOND, () => ball.Velocity = new Utility.Vector2D(0, BALL_SPEED));
        }

        private void floatPoints(Brick brick)
        {
            if (brick.Value > 0)
            {
                // calculate point tile to show
                int tile = POINT_TILE + ((brick.Hits - 1) * 2);

                // create and setup floating point
                GameObject pointFloater = new GameObject(brick.X, brick.Y, Tileset.Texture, Tileset.GetTile(tile), ghost: false);
                pointFloater.Velocity = new Vector2D(0, -2);

                // animate point floater
                Animation animation = AddAnimation(new Animation(
                    this,
                    pointFloater,
                    new List<Rectangle>
                    {
                    Tileset.GetTile(tile),
                    Tileset.GetTile(tile + 1)
                    },
                    Tileset,
                    Time.TENTH_SECOND,
                    loop: true
                ));
                animation.Animating = true;

                // show point floater for half a second
                AddGameObject(pointFloater);
                QueueTask(Time.HALF_SECOND, () => QueueFree(pointFloater));
            }
        }

        public override void StartGame()
        {
            if (!HasInfiniteLives)
            {
                AddGameObject(livesLabel);
                foreach (GameObject life in lifeDisplay)
                    AddGameObject(life);
            }

            AddGameObject(backdrop);
            AddGameObject(paddle);
            AddGameObject(ball);

            AddGameObject(timeLabel);
            AddGameObject(gameTime);

            AddGameObject(scoreLabel);
            updateScore();

            currentLevel = 0;
            CurrentLevel.Build();
            levelRunning = true;

            gameStopwatch.Start();
            StartBall();
        }

        protected override void SaveGame()
        {
            // save persistant data (high score, level?)...
        }

        public override void PlaySound(Stream sound)
        {
            if (HasSfx) base.PlaySound(sound);
        }

        public override void EndGame()
        {
            gameStopwatch.Stop();  

            QueueTask(Time.SECOND, () =>
            {
                // free groups of objects
                balls.ForEach(b => QueueFree(b));
                lifeDisplay.ForEach(l => QueueFree(l));
                CurrentLevel.Bricks.ForEach(b => QueueFree(b));

                // reset lives
                lifes = START_LIFES;
                foreach (Animation heartbreak in heartbreak)
                    heartbreak.Reset();

                // reset score and time
                score = 0;
                levelRunning = false;
                gameStopwatch.Reset();

                // remove any augmentation
                if (!(currentAugment is null))
                {
                    // reject and remove the current augment
                    currentAugment.Reject();
                    ClearAugment();
                }

                // free game objects
                QueueFree(livesLabel);
                QueueFree(scoreDisplay);
                QueueFree(scoreLabel);
                QueueFree(timeLabel);
                QueueFree(gameTime);
                QueueFree(Paddle);
                QueueFree(backdrop);

                // return to menu
                AddGameObject(menu);
                menu.Open();

                PlaySound(Properties.Resources.exit);
            });
        }
    }
}
