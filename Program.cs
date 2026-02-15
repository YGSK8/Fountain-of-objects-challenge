using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Text;


Player player = new Player();
Position entrance = new Position(0,0);
Position fountain = new Position (1,0);
Map map = new Map(4,4,entrance,fountain);
Game game = new Game(player,map);
game.Play();
public record Position (int X,int Y);

public static class InputValidator<T>
{
    public static T ValidInput(T[] list)
    {
        Console.Write("Your options are: ");
        foreach(T item in list)Console.Write(item.ToString()+" ");
        Console.WriteLine("");
        bool valid = false;
        T? value = default;
        while(valid == false)
        {
            string input = Console.ReadLine();
            foreach(T item in list)
            {
                if(input.ToLower() == item.ToString().ToLower())
                {
                    valid = true;
                    value = item;
                } 
            }
        }
        return value;
    }
}
public static class List<T>{
    public static T[] RemoveItem(T[]list, T item)
    {
        T[]updated = new T[list.Length-1];
        int uindex = 0;
        for(int index = 0; index < list.Length; index++)
        {
            if(index != Array.IndexOf(list, item))
            {
                updated[uindex]= list[index];
                uindex++;
            }
        }
        return updated;
    }
    public static T[] AddItem(T[]List,T[]Items)
    {
        T[]updated = new T[List.Length + Items.Length];
        for(int index =0;index < List.Length;index++) updated[index] = List[index];
        for(int index =0;index <Items.Length;index++)updated[^(index+1)] = Items[^(index+1)];
        return updated;
    }
}



public enum Command {north, south, east, west, exit,activate, deactivate}
public class Player
{
    public string Name{get;init;}
    public Position Position{get;set;} = new Position(0,0);
    private Command _inputcommand;

    public Command GetCommand() => _inputcommand;
    public Command Input(Map map)
    {

        Command [] list = map.Commands(map.Layout[Position.X, Position.Y]);
       Command input = InputValidator<Command>.ValidInput(list);
       _inputcommand = input;
       return input;
    }
    public void UpdatePosition ()
    {

        Position = _inputcommand switch
        {
            Command.north => Position with {X = Position.X-1},
            Command.south => Position with {X = Position.X+1},
            Command.east => Position with {Y = Position.Y+1},
            Command.west => Position with {Y = Position.Y-1},
            _ => Position
        };
    }

}

public class Room
{
    public Position Position {get;}
    public Room(int row, int col)
    {
        Position = new Position(row,col);
    }
    public virtual void Effect(){}
}

public class Fountain : Room
{
    public bool Enabled{get;set;} = false;
    public Fountain(int row, int col):base(row, col){}
    public override void Effect()
    {
        string text = Enabled switch
        {
            true => "You hear the rushing waters from the fountain of objects, it has been reactivated! Now exit the cave!",
            false => "You hear water dripping in this room, the fountain of objects is in this room!"
        };
        if (Enabled == false)
        {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine(text);
        Console.ResetColor();
        }
        else if (Enabled == true)
        {
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine(text+"\n");
            Console.ResetColor();
            Console.WriteLine("");
        }

    }

}

public class Entrance: Room
{
    public Entrance(int row, int col):base(row,col){}
    public override void Effect()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("You see light from outside the cave, you are at the entrance");
        Console.ResetColor();
    }
}


public class Map
{
    public Room[,] Layout{get;}
    public Position Entrance{get;}
    public Position Fountain{get;}
    public Room GetRoom(Position position) => Layout[position.X, position.Y];
    public Map(int rowcount, int colcount, Position entrance, Position fountain )//--To initialize map, neep to provide dimensions of array and position of entrance and fountain room.
    {
        if (rowcount >=2 && colcount >= 2)//-- Ensures you cannot create a 2D array of 1X1 whic is only one element.
        {
        Entrance = entrance;//-- initialize Entrance position
        Fountain = fountain;//-- initialize Fountain position
        Layout = new Room [rowcount,colcount];
        for(int x = 0; x < Layout.GetLength(0); x++)
        {
            for(int y = 0; y < Layout.GetLength(1); y++)
            {
                Layout[x,y]=new Room(x,y);
            }
        }   
        }
        //-- Replace regular Room at Entrance & Fountain positions by Specialized rooms
        Layout[entrance.X,entrance.Y] = new Entrance(entrance.X,entrance.Y);
        Layout[fountain.X, fountain.Y] = new Fountain(fountain.X,fountain.Y);
    }

   public Command[] Commands(Room room) //- Method that provides available commands to a player based on their position
    {
        Command [] commands = [Command.north, Command.south, Command.east,Command.west];
        Command [] updated = commands; //--Do a first check on room row to see if player can move north or south. Remove command north or south depending on row position.
        if(room.Position.X == 0) updated = List<Command>.RemoveItem(commands,Command.north);
        else if(room.Position.X == Layout.GetLength(0)-1) updated = List<Command>.RemoveItem(commands,Command.south);
        Command [] updated2 = updated; //-Do a second check on room col to see if player can move east or west. Remove command east or west depending on col positiion.
        if(room.Position.Y == 0) updated2 = List<Command>.RemoveItem(updated, Command.west);
        else if(room.Position.Y == Layout.GetLength(1)-1) updated2 = List<Command>.RemoveItem(updated, Command.east);
        if(room is Fountain)
        {
        updated2 = List<Command>.AddItem(updated2,[Command.activate,Command.deactivate]);
        }
        if(room is Entrance)
        {
        updated2 = List<Command>.AddItem(updated2,[Command.exit]);    
        }
        return updated2;
    }

}

public class Game
{
     private bool _win = false;
     private Player _player;
     private Map _map;
     private bool _fountainactivated;

     public Game (Player player, Map map)
    {
        _player = player;
        _map = map;
        _win = false;
        _fountainactivated = false;
    }
    public void DisplayMap()
    {
        int x = 0;
        foreach(Room room in _map.Layout)
        {
            if(room.Position.X == x)
            {
                if(room.Position.X == _player.Position.X && room.Position.Y == _player.Position.Y)Console.Write("X ");
                else Console.Write(". ");
            }
            else if (room.Position.X != x)
            {
                x++;
                if(room.Position.X == _player.Position.X && room.Position.Y == _player.Position.Y){Console.Write("\nX ");}
                else Console.Write("\n. ");          
            }
        }
        Console.WriteLine("");
    }

    private void WinCondition()
    {
        if(_player.GetCommand() == Command.exit && _fountainactivated == true){ _win = true; Console.WriteLine("You win!");}
    }
    private void RoomEffect()
    {
        Room playerRoom = _map.Layout[_player.Position.X,_player.Position.Y];
        // Console.WriteLine($"You are at {_player.Position.X},{_player.Position.Y}");
        playerRoom.Effect();

    }
    private void RoomAction(Command input)
    {
        if( input == Command.activate)
            {
                Fountain fountain = (Fountain)_map.Layout[_map.Fountain.X,_map.Fountain.Y];
                fountain.Enabled = true;
                _fountainactivated = true;
            }
        else if (input == Command.deactivate)
        {
                Fountain fountain = (Fountain)_map.Layout[_map.Fountain.X,_map.Fountain.Y];
                fountain.Enabled = false;
                _fountainactivated = false;
        }
        else if (input == Command.exit){Environment.Exit(0);}
    }
    public void Play()
    {

        while(_win == false)
        {
            RoomEffect();
             DisplayMap();
            Command input = _player.Input(_map);
            WinCondition();
            RoomAction(input);
            _player.UpdatePosition();
            
        }
    }

}