"""Example script using cosmos_client Client to communicate with the Cosmos host.

This mirrors the echo.py test pattern but uses the Client SDK instead of
raw sys.stdout/sys.stdin calls.
"""

from cosmos_client import Client


def main():
    # Send first request and read reply
    response1 = Client.Send(Client.Log("request1"))
    # response1.message contains the host's reply

    # Send second request and read reply
    response2 = Client.Send(Client.Log("request2"))
    # response2.message contains the host's reply


if __name__ == "__main__":
    main()
