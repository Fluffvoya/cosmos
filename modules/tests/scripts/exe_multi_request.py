"""Test script for ExecuteProcess: sends two requests and reads replies, like echo.py."""
import sys

sys.stdout.write("request1\n")
sys.stdout.flush()
reply1 = sys.stdin.readline()

sys.stdout.write("request2\n")
sys.stdout.flush()
reply2 = sys.stdin.readline()
