"""Test script: sends requests to the host and reads replies."""
import sys

# Send first request and read reply
sys.stdout.write("request1\n")
sys.stdout.flush()
reply1 = sys.stdin.readline()

# Send second request and read reply
sys.stdout.write("request2\n")
sys.stdout.flush()
reply2 = sys.stdin.readline()
