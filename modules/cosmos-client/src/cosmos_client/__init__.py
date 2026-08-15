"""Cosmos Python client SDK.

Provides a Client class for sending requests to and receiving responses
from the Cosmos host application via stdin/stdout JSON communication.
"""

from cosmos_client.client import Client
from cosmos_client.request import Request
from cosmos_client.response import Response

__all__ = ["Client", "Request", "Response"]
