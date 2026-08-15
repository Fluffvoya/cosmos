"""Response model for Cosmos host communication."""

from __future__ import annotations

import json
from dataclasses import dataclass


@dataclass
class Response:
    """Represents a response received from the Cosmos host.

    Attributes:
        request: The name of the original request action.
        message: The response message content.
    """

    request: str
    message: str

    def Serialize(self) -> str:
        """Serialize this response to a JSON string.

        Returns:
            A JSON string representation of the response.

        Raises:
            TypeError: If the response cannot be serialized.
        """
        return json.dumps({"request": self.request, "message": self.message})

    @classmethod
    def Deserialize(cls, text: str) -> Response | None:
        """Deserialize a JSON string into a Response.

        Args:
            text: The JSON string to deserialize.

        Returns:
            A Response instance, or None if the text is empty.
        """
        if not text:
            return None

        data = json.loads(text)
        return cls(request=data["request"], message=data.get("message", ""))
