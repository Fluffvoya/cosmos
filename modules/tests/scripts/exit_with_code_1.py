"""Test script: writes to stderr and exits with code 1."""
import sys
sys.stderr.write("script error\n")
sys.exit(1)
