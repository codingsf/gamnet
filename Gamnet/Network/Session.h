#ifndef GAMNET_NETWORK_SESSION_H_
#define GAMNET_NETWORK_SESSION_H_

#include <map>
#include <mutex>
#include <list>
#include <boost/asio.hpp>
#include <deque>
#include "Packet.h"
#include "HandlerContainer.h"

namespace Gamnet { namespace Network {

class IListener;
class Session : public std::enable_shared_from_this<Session>
{
public :
	struct Init
	{
		template <class T>
		T* operator() (T* session)
		{
			session->socket_.close();
			session->sessionKey_ = 0;
			session->listener_ = NULL;
			session->readBuffer_ = Packet::Create();
			session->sendBuffers_.clear();
			session->lastHeartBeatTime_ = ::time(NULL);
			session->handlerContainer_.Init();
			return session;
		}
	};

	boost::asio::ip::tcp::socket socket_;
	boost::asio::strand strand_;
	boost::asio::ip::address remote_address_;
	uint64_t sessionKey_;
	IListener* listener_;
	std::shared_ptr<Packet> readBuffer_;
	std::deque<std::shared_ptr<Packet>> sendBuffers_;
	time_t lastHeartBeatTime_;
	HandlerContainer handlerContainer_;

	Session();
	virtual ~Session();

	virtual void OnAccept() = 0;
	virtual void OnClose(int reason) = 0;
	virtual void OnError(int reason);

	virtual void AsyncRead();
	void AsyncSend(std::shared_ptr<Packet> packet);
	void AsyncSend(const char* buf, int len);
	int SyncSend(std::shared_ptr<Packet> buffer);
	int SyncSend(const char* buf, int len);

	int Send(std::shared_ptr<Packet> buffer);
	int Send(const char* buf, int len);

	void SetListener(IListener* listener);
private :
	void FlushSend();
};
}}
#endif /* NETWORK_SESSION_H_ */
