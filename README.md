# volatile
A password saver in .NET. Saves passwords inside the executable in AES256.

# Features
- Add hint, username, password, and email address to some websites
- Delete, add and update websites
- Export data to JSON

# How to use?
#### ``volatile [password] [-w --website] [-u username -p password -e email -h hint] [-v, --verbose] [-x, --export] [-r, --remove]``

- If a website is given,
   - If any other argument is given, update data
   - Else, return data
- Else, Volatile will enter interactive mode

# Interactive mode
Available commands:
- `get [website]` return data associated to website
- `set [website -u username -p password -e email -h hint]` add data associated to website
- `delete [website]` remove data associated to website
- `export [filename]` export json data to a file. if no name is given, print the json directly
- `exit` exit Volatile
- `clear` clear the screen
- `list` list all saved websites and their data

# Notes
- A website can be optionally passed with `export`.
- The password will be set when you use volatile for the first time.

# How does it work?
1. When Volatile is started, it loads two of its embedded resources: a Base64 compiled version of VolatileExe, and a Base64 string of data.
2. It tries to decrypt the data using AES256.
3. All data is saved in memory
4. When quitting, a temporary file with the content of VolatileExe is created and executed. This file is executed, and the data in Volatile is encrypted and sent to VolatileExe using named pipes.
5. Volatile exits, and VolatileExe replaces the embedded string of data inside Volatile by the one it received earlier.
