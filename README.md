# Compile Quest
This project is an educational Unity game that teaches students coding concepts and structure in Python with the use of a built in AI assistant. It is a space themed game that covers individual coding topics using planets, and each planet consists of 15 levels which are fully functional and individually designed. As a player, you will write input code or solve parsons problems to help navigate Cosmo, the game's character, to reach the goal. 

## Game Structure
Each planet covers an individual topic. Currently there are three fully developed planets: 
- **Declan**, which covers declaring variables and using simple move commands. 
- **Variablis**, which builds off of declaring variables, and has the player use those to call functions. 
- **Condinal**, which introduces the player to conditional statements
- ***More coming soon...***

Each planet consists of 15 levels, each being unique and having a new problem for the player to solve. The levels had objectives like gems to collect, and certain constraints like a certain amount of lines of code to use. For each level, it will be structured as the following:

- The game frame is on the left, with the interactable section on the right.
- Below the game frame is where objective will be displayed, complete all of these to win.
- Below the interactable area on the right is where the player can run their input to see if it is correct, as well as the AI button. 
- The AI button will send your current input to our server where the AI model will process it and send feedback response. This process usually takes 1-2 seconds or less. 

## Level Design

There are two main types of levels, inputs problems and parsons problems.
#### **Inputs Problems**  
   - 2D platformer levels with a code box on the right
   - The player has to help Cosmo navigate through the level by typing lines of code into the box

#### **Parsons Problems**  
   - Galaga styled levels with blocks instead of text based input
   - Align the code blocks in the correct order, then use the ship controls to complete the level
   - The goal is to defeat the enemy ship by programming yours to be strong enough



## Features

- **AI-Powered Feedback:** Uses Groq AI service which utilizes llama 3.3-70b versatile to generate adaptive feedback
- **Registered and Encrypted Domain:** The game is hosted entirely on a purchased domain, and all data going in and out is encrypted and safe
- **Session Management:** Between playing, your data will be saved to our database, so you can stop whenever you like and pick up where you left off when you are ready
- **Remove Server:** We use an AWS EC2 server to host our AI processing so there are no limitations on your machine or website strain and everything communicates over https
- **Progression:** Each planet starts simple and gets more difficult as you progress to create a learning curve
- **Lenient evaluation:** Students can be creative, as long as the code behaves as expected
- **Cumulative context:** Previous exercises carry over, so students can reference earlier code
- **Clean, polished interface:** Large fonts, bordered code input, highlighted buttons, and clear feedback areas

---

## Requirements

A working internet connection and access to a web browser, then go to:
#### https://compilequest.org


When you launch the game, register by entering a Username and a Password and you can get right into the game. As long as you remember your credentials, your session will be saved. 