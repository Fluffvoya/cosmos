"""Test script for ExecuteProcess: sends a request via stdout and reads the reply from stdin."""
import sys

# Send request (host reads this from our stdout)
sys.stdout.write("request1\n")
sys.stdout.flush()

# Read reply (host writes this to our stdin)
reply = sys.stdin.readline()
