"""Client for communicating with the Cosmos host application.

The Client provides static methods to build Request objects and an instance
method to send them via stdout and receive responses via stdin.
"""

from __future__ import annotations

import sys

from cosmos_client.request import Request
from cosmos_client.response import Response


class Client:
    """Client for the Cosmos host IPC protocol.

    Static methods create Request objects. Use Send() to write a request
    to stdout and read the response from stdin.
    """

    # ── Static request builders ─────────────────────────────────────

    @staticmethod
    def Log(content: str) -> Request:
        """Create a Log request.

        Args:
            content: The log message content.

        Returns:
            A Request for logging.
        """
        return Request(request="Log", args=[content])

    @staticmethod
    def Warning(content: str) -> Request:
        """Create a Warning request.

        Args:
            content: The warning message content.

        Returns:
            A Request for a warning.
        """
        return Request(request="Warning", args=[content])

    @staticmethod
    def Error(content: str) -> Request:
        """Create an Error request.

        Args:
            content: The error message content.

        Returns:
            A Request for an error.
        """
        return Request(request="Error", args=[content])

    @staticmethod
    def GetUserName() -> Request:
        """Create a GetUserName request.

        Returns:
            A Request to get the current user name.
        """
        return Request(request="GetUserName")

    @staticmethod
    def MessageBox(name: str, message: str) -> Request:
        """Create a MessageBox request.

        Args:
            name: The message box title/name.
            message: The message content.

        Returns:
            A Request to show a message box.
        """
        return Request(request="MessageBox", args=[name, message])

    @staticmethod
    def MessageBar(message: str, level: str) -> Request:
        """Create a MessageBar request.

        Args:
            message: The message content.
            level: The message level (e.g. "info", "warning", "error").

        Returns:
            A Request to show a message bar.
        """
        return Request(request="MessageBar", args=[message, level])

    @staticmethod
    def PlayRingtone(audio_path: str) -> Request:
        """Create a PlayRingtone request.

        Args:
            audio_path: Path to the audio file to play.

        Returns:
            A Request to play a ringtone.
        """
        return Request(request="PlayRingtone", args=[audio_path])

    @staticmethod
    def PlayRingtoneOnce(audio_path: str) -> Request:
        """Create a PlayRingtoneOnce request.

        Plays the audio once without looping, then auto-closes.

        Args:
            audio_path: Path to the audio file to play.

        Returns:
            A Request to play a ringtone once.
        """
        return Request(request="PlayRingtoneOnce", args=[audio_path])

    @staticmethod
    def OpenRegisteredApp(app_name: str) -> Request:
        """Create an OpenRegisteredApp request.

        Args:
            app_name: Name of the registered application to open.

        Returns:
            A Request to open a registered app.
        """
        return Request(request="OpenRegisteredApp", args=[app_name])

    # ── Serialization helpers ───────────────────────────────────────

    @staticmethod
    def CreateRequest(request: Request) -> str:
        """Serialize a Request to a JSON string.

        Args:
            request: The Request to serialize.

        Returns:
            JSON string representation of the request.
        """
        return request.Serialize()

    @staticmethod
    def GetResponse(text: str) -> Response | None:
        """Deserialize a JSON string into a Response.

        Args:
            text: The JSON string to deserialize.

        Returns:
            A Response instance, or None if the text is empty.
        """
        return Response.Deserialize(text)

    @staticmethod
    def GetResponseMessage(text: str) -> str | None:
        """Deserialize a response and extract the message.

        Args:
            text: The JSON string to deserialize.

        Returns:
            The message string, or None if deserialization fails.
        """
        response = Response.Deserialize(text)
        return response.message if response else None

    # ── I/O ─────────────────────────────────────────────────────────

    @staticmethod
    def Send(request: Request) -> Response:
        """Send a request to the host and return the response.

        Writes the serialized request as a JSON line to stdout, flushes,
        then reads one JSON line from stdin as the response.

        Args:
            request: The Request to send.

        Returns:
            The Response from the host.

        Raises:
            ConnectionError: If stdin/stdout communication fails.
        """
        try:
            sys.stdout.write(request.Serialize() + "\n")
            sys.stdout.flush()
        except Exception as e:
            raise ConnectionError(f"Failed to send request: {e}") from e

        try:
            line = sys.stdin.readline()
        except Exception as e:
            raise ConnectionError(f"Failed to read response: {e}") from e

        if not line:
            raise ConnectionError("Host closed the connection (empty response).")

        response = Response.Deserialize(line.strip())
        if response is None:
            raise ConnectionError(f"Invalid response from host: {line!r}")

        return response
