const joinedPlayers = [];

const handleMessage = (ws, message) => {
    const data = JSON.parse(message);
    switch (data.command) {
        case 'join':
            console.log('Player joined the game:', data.playerId);
            // Handle player joining
            joinedPlayers.push({ id: data.playerId, name: "TestName" });
            break;
        case 'move':
            console.log('Player moved:', data.playerId, data.input);
            // Handle player movement
            break;
        case 'leave':
            console.log('Player left the game:', data.playerId);
            // Handle player leaving
            const index = joinedPlayers.findIndex(player => player.id === data.playerId);
            if (index !== -1) {
                joinedPlayers.splice(index, 1);
            }
            break;
        // Add more cases for other commands
        default:
            console.log('Unknown command:', data.command);
            break;
    }
};

const handleDisconnect = (ws) => {
    // Handle player disconnection
};

const getJoinedPlayers = () => {
    return joinedPlayers.map(player => ({ id: player.id, name: player.name }));
};

module.exports = { handleMessage, handleDisconnect, getJoinedPlayers };