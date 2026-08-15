"""Request model for Cosmos host communication."""

from __future__ import annotations

import json
from dataclasses import dataclass, field


@dataclass
class Request:
    """Represents a request sent to the Cosmos host.

    Attributes:
        request: The name of the request action (e.g. "Log", "GetUserName").
        args: A list of string arguments for the request.
    """

    request: str
    args: list[str] = field(default_factory=list)

    def Serialize(self) -> str:
        """Serialize this request to a JSON string.

        Returns:
            A JSON string representation of the request.

        Raises:
            TypeError: If the request cannot be serialized.
        """
        return json.dumps({"request": self.request, "args": self.args})

    @classmethod
    def Deserialize(cls, text: str) -> Request | None:
        """Deserialize a JSON string into a Request.

        Args:
            text: The JSON string to deserialize.

        Returns:
            A Request instance, or None if the text is empty.
        """
        if not text:
            return None

        data = json.loads(text)
        return cls(request=data["request"], args=data.get("args", []))
